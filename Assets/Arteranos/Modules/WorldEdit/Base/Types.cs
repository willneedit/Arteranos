/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using System.Collections.Generic;
using ProtoBuf;

using Arteranos.Core;
using UnityEngine;
using System.IO;
using System;
using System.Threading;
using Ipfs.Unity;
using Arteranos.Services;
using GLTFast;

namespace Arteranos.WorldEdit
{
    public enum ColliderType
    {
        Ghostly = 5,     // (Layer: UI) Collides nothing, passable
        Intangible = 14, // (Layer: RigidBody): Collides likewise and solids, passable
        Solid = 0,       // (Layer: Default): Collides likewise and intangibles, stops avatars
    }

    // -------------------------------------------------------------------
    #region World Edit Snapshot and Restore

    [ProtoContract]
    public class WorldDecoration : IWorldDecoration
    {
        [ProtoMember(1)]
        public WorldInfoNetwork info;

        [ProtoMember(2)]
        public List<WorldObject> objects;

        public WorldInfoNetwork Info { get => info; set => info = value; }
        public IEnumerator BuildWorld()
        {
            Transform t = WorldEditorData.FindObjectByPath(null);

            G.WorldEditorData.ClearBlueprints();

            for (int i = 0; i < objects.Count; i++)
                yield return objects[i].Instantiate(t);
        }

        public void TakeSnapshot()
        {
            Transform t = WorldEditorData.FindObjectByPath(null);
            for (int i = 0; i < t.childCount; i++)
                objects.Add(t.GetChild(i).MakeWorldObject());
        }
    }
    #endregion
    // -------------------------------------------------------------------
    #region Generalized World Object description

    [ProtoContract]
    public class WorldObject
    {
        [ProtoMember(1)]
        public WorldObjectAsset asset;      // see above

        [ProtoMember(2)]
        public string name;

        [ProtoMember(3)]
        public Guid id;

        [ProtoMember(4)]
        public bool collidable;

        [ProtoMember(7)]
        public List<WOCBase> components;  // Additional properties (like teleport marker, ...)

        [ProtoMember(8)]
        public List<WorldObject> children;  // grouped objects


        public GameObject GameObject { get; private set; } = null;

        public WorldObject()
        {
            Init();
        }

        public WorldObject(PrimitiveType primitive)
        {
            Init();
            asset = new WOPrimitive { primitive = primitive };
        }

        public WorldObject(WorldObjectAsset asset, string name)
        {
            Init();
            this.asset = asset;
            this.name = name;
        }

        private void Init()
        {
            id = Guid.NewGuid();

            components = new();

            children = new();
        }

        public void Serialize(Stream stream)
            => Serializer.Serialize(stream, this);

        public static WorldObject Deserialize(Stream stream)
            => Serializer.Deserialize<WorldObject>(stream);

        public IEnumerator Instantiate(Transform parent, Action<GameObject> callback = null)
        {

            static IEnumerator LoadglTF(WOglTF WOglTF, GameObject LoadedObject)
            {
                using CancellationTokenSource cts = new(60000);
                byte[] data = null;
                yield return Asyncs.Async2Coroutine(
                    G.IPFSService.ReadBinary(WOglTF.glTFCid, cancel: cts.Token),
                    _data => data = _data);

                if (data == null)
                    yield break;

                GltfImport gltf = new(deferAgent: new UninterruptedDeferAgent());

                bool success = false;

                yield return Asyncs.Async2Coroutine(
                    gltf.LoadGltfBinary(data, cancellationToken: cts.Token),
                    _success => success = _success);

                if (success)
                {
                    GameObjectInstantiator instantiator = new(gltf, LoadedObject.transform);

                    yield return Asyncs.Async2Coroutine(
                        gltf.InstantiateMainSceneAsync(instantiator));
                }

                LoadedObject.name = $"glTF {WOglTF.glTFCid}";
            }

            static IEnumerator LoadKit(WOKitItem kitItem, GameObject LoadedObject)
            {
                LoadedObject.name = $"kit {kitItem.kitCid}, Item {kitItem.kitName}";

                throw new NotImplementedException();
            }

            GameObject gob;
            if (asset == null)
            {
                gob = new GameObject("Empty world object");
                gob.SetActive(false);
            }
            else
            {
                // Look up, or create a blueprint if none exist
                if (!G.WorldEditorData.TryGetBlueprint(asset, out GameObject gobbo))
                {
                    // TODO: Implement kit item asset instantiation
                    switch (asset)
                    {
                        case WOPrimitive WOPR: // Pun intended :)
                            gobbo = GameObject.CreatePrimitive(WOPR.primitive);
                            gobbo.TryGetComponent(out Renderer renderer);
                            renderer.material = BP.I.WorldEdit.DefaultWEMaterial;
                            gobbo.SetActive(false);
                            break;
                        case WOglTF WOglTF:
                            gobbo = new GameObject("Unleaded glTF world object"); // :)
                            gobbo.SetActive(false);
                            yield return LoadglTF(WOglTF, gobbo);
                            break;
                        case WOKitItem WOKitItem:
                            gobbo = new GameObject("Unleaded kit world object"); // :)
                            gobbo.SetActive(false);
                            yield return LoadKit(WOKitItem, gobbo);
                            break;
                        default:
                            gobbo = new GameObject("Unsupported world object");
                            gobbo.SetActive(false);
                            break;
                    }

                    gobbo.transform.position = new Vector3(0, -9000, 0);
                    G.WorldEditorData.AddBlueprint(asset, gobbo);
                }

                // Got it now, clone it.
                gob = UnityEngine.Object.Instantiate(gobbo, Vector3.zero, Quaternion.identity);
            }

            gob.name = name;

            WorldObjectComponent woc = gob.AddComponent<WorldObjectComponent>();  
            woc.Asset = asset;
            woc.Id = id;
            woc.IsCollidable = collidable;
            woc.WOComponents = components;

            Transform t = gob.transform;
            t.SetParent(parent);

            foreach (WOCBase w in woc.WOComponents)
            {
                w.GameObject = gob;
                w.CommitState();
            }

            GameObject = gob;
            gob.SetActive(true);

            foreach (WorldObject child in children)
                yield return child.Instantiate(t);

            yield return null;

            callback?.Invoke(gob);
        }

        public void Patch()
        {
            GameObject gob = GameObject;

            if(gob == null)
                throw new ArgumentNullException(nameof(gob));

            if (!gob.TryGetComponent(out WorldObjectComponent woc))
                throw new ArgumentException("GameObject is not a World Object's instantiation");

            if (id != woc.Id)
                throw new ArgumentException("Mismatched object ID in patching");

            gob.name = name;
            for (int i = 0; i < components.Count; i++)
                woc.AddOrReplaceComponent(components[i]);
        }

        public T GetWComponent<T>() where T : WOCBase
        {
            foreach(WOCBase w in components)
                if(w is T woc) return woc;
            return null;
        }

    }

    #endregion
    // -------------------------------------------------------------------
    #region World Edit Patch operations

    [ProtoContract]
    public class WorldObjectInsertion : WorldChange
    {
        [ProtoMember(1)]
        public WorldObjectAsset asset;      // see above

        [ProtoMember(2)]
        public string name;

        [ProtoMember(3)]
        public Guid id = Guid.NewGuid();

        [ProtoMember(4)]
        public bool collidable;

        [ProtoMember(7)]
        public List<WOCBase> components;  // Additional properties (like teleport marker, ...)

        public override IEnumerator Apply()
        {
            Transform t = FindObjectByPath();

            WorldObject worldObject = new()
            {
                asset = asset,
                name = name,
                id = id,
                collidable = collidable,
                components = components
            };

            yield return worldObject.Instantiate(t);
        }
    }

    [ProtoContract]
    public class WorldObjectPatch : WorldChange
    {
        [ProtoMember(2)]
        public List<WOCBase> components;

        public override IEnumerator Apply()
        {
            Transform t = FindObjectByPath();

            t.TryGetComponent(out WorldObjectComponent cur_woc);

            // NB: Protobuf omits empty lists, rendering them as null while deserialization.
            for (int i = 0; i < components?.Count; i++)
                cur_woc.AddOrReplaceComponent(components[i]);

            yield return null;
        }
    }

    [ProtoContract]
    public class WorldObjectDeletion : WorldChange
    {
        // Nothing more to need
        public override IEnumerator Apply()
        {
            Transform t = FindObjectByPath();

            // Unhook the object from the hierarchy first because we consider it deleted,
            // even if it's not yet destroyed in the current frame.
            t.SetParent(null);
            UnityEngine.Object.Destroy(t.gameObject);

            yield return null;
        }
    }

    [ProtoContract]
    public class WorldRollbackRequest : WorldChange
    {
        [ProtoMember(2)]
        public string hash;
        public override IEnumerator Apply()
        {
            yield return G.WorldEditorData.RecallUndoState(hash);
        }
    }

    #endregion
    // -------------------------------------------------------------------
    #region World Edit Patch root

    [ProtoContract]
    [ProtoInclude(65537, typeof(WorldObjectInsertion))]
    [ProtoInclude(65538, typeof(WorldObjectPatch))]
    [ProtoInclude(65539, typeof(WorldObjectDeletion))]
    [ProtoInclude(65540, typeof(WorldRollbackRequest))]
    public abstract class WorldChange : IWorldChange
    {
        [ProtoMember(1)]
        public List<Guid> path;

        public void Serialize(Stream stream)
            => Serializer.Serialize(stream, this);

        public static WorldChange Deserialize(Stream stream)
            => Serializer.Deserialize<WorldChange>(stream);

        protected Transform FindObjectByPath() => WorldEditorData.FindObjectByPath(path);

        public abstract IEnumerator Apply();

        public void SetPathFromThere(Transform t)
        {
            path = new();
            while (t.TryGetComponent(out WorldObjectComponent woc))
            {
                path.Add(woc.Id);
                t = t.parent;
            }
            path.Reverse();
        }

        public void EmitToServer()
        {
#if UNITY_EDITOR
            // Shortcut in 'lean' setup/test scene.
            if(SettingsManager.Instance == null)
            {
                G.WorldEditorData.DoApply(this);
                return;
            }
#endif
            using MemoryStream ms = new();
            Serialize(ms);
            CTSWorldObjectChange cts_wc = new() { changerequest = ms.ToArray() };
            SettingsManager.EmitToServerCTSPacket(cts_wc);
        }
    }

    #endregion
    // -------------------------------------------------------------------
    public static class GameObjectExtensions
    {
        internal static WorldObject MakeWorldObject(this Transform t, bool includeChildren = true)
        {
            WorldObject wo = new();

            if (t.TryGetComponent(out WorldObjectComponent woc))
                wo.asset = woc.Asset;
            else return null; // filter out alien GameObjects

            wo.name = t.name;
            wo.components = woc.WOComponents;
            wo.id = woc.Id;
            wo.collidable = woc.IsCollidable;

            if(includeChildren)
            {
                for (int i = 0; i < t.childCount; ++i)
                {
                    WorldObject item = MakeWorldObject(t.GetChild(i));
                    if (item != null) wo.children.Add(item);
                }
            }

            return wo;
        }

        public static WorldObjectPatch MakePatch(this Transform t, bool complete = false)
        {
            if (!t.TryGetComponent(out WorldObjectComponent woc))
                throw new ArgumentException("GameObject is not in the world object hierarchy");

            return woc.MakePatch(complete);
        }

        public static WorldObjectInsertion MakeInsertion(this Transform t)
        {
            if (!t.TryGetComponent(out WorldObjectComponent woc))
                throw new ArgumentException("GameObject is not in the world object hierarchy");

            return woc.MakeInsertion();
        }

        public static WorldObject MakeWorldObject(this GameObject go, bool includeChildren = true)
            => go.transform.MakeWorldObject(includeChildren);

        public static WorldObjectPatch MakePatch(this GameObject go, bool complete = false)
            => go.transform.MakePatch(complete);

        public static WorldObjectInsertion MakeInsertion(this GameObject go)
            => go.transform.MakeInsertion();
    }
}
