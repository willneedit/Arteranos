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

    [ProtoContract]
    public class WorldDecoration
    {
        [ProtoMember(1)]
        public WorldInfoNetwork info;

        [ProtoMember(2)]
        public List<WorldObject> objects;
    }

    [ProtoContract]
    public class WorldObject
    {
        [ProtoMember(1)]
        public WorldObjectAsset asset;      // see above

        [ProtoMember(2)]
        public string name;

        [ProtoMember(3)]
        public Guid id;

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

            components = new()
            {
                new WOCTransform(),
                new WOCColor()
            };

            foreach(WOCBase w in components) w.Init();

            children = new();
        }

        public void Serialize(Stream stream)
            => Serializer.Serialize(stream, this);

        public static WorldObject Deserialize(Stream stream)
            => Serializer.Deserialize<WorldObject>(stream);

        public IEnumerator Instantiate(Transform parent, Action<GameObject> callback = null, WorldEditorData editorData = null)
        {
            IEnumerator LoadglTF(string GLTFObjectPath, GameObject LoadedObject)
            {
                using CancellationTokenSource cts = new(60000);
                byte[] data = null;
                yield return Asyncs.Async2Coroutine(
                    IPFSService.ReadBinary(GLTFObjectPath, cancel: cts.Token),
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
            }

            GameObject gob;

            // TODO: Implement kit item asset instantiation
            if (asset is WOPrimitive WOPR)                          // Pun intended :)
            {
                gob = GameObject.CreatePrimitive(WOPR.primitive);
                gob.SetActive(false);
            }
            else if(asset is WOglTF WOglTF)
            {
                gob = new GameObject("Unleaded glTF world object"); // :)
                gob.SetActive(false);
                yield return LoadglTF(WOglTF.glTFCid, gob);
            }
            else
            {
                gob = new GameObject("Empty or unsupported world object");
                gob.SetActive(false);
            }

            // More complex constructs can be put as a child of an empty GameObject.

            gob.name = name;

            WorldObjectComponent woc = gob.AddComponent<WorldObjectComponent>();  
            woc.Asset = asset;
            woc.Id = id;
            woc.WOComponents = components;
            woc.EditorData = editorData;

            Transform t = gob.transform;
            t.SetParent(parent);

            foreach (WOCBase w in woc.WOComponents)
            {
                w.Awake(gob);
                w.CommitState();
            }

            GameObject = gob;
            gob.SetActive(true);

            foreach (WorldObject child in children)
                yield return child.Instantiate(t, editorData: editorData);

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

    // -------------------------------------------------------------------

    [ProtoContract]
    public class WorldObjectInsertion : WorldChange
    {
        [ProtoMember(1)]
        public WorldObjectAsset asset;      // see above

        [ProtoMember(2)]
        public string name;

        [ProtoMember(3)]
        public Guid id;                     // ID of the new object, assigned by the server

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

        public void Serialize(Stream stream)
            => Serializer.Serialize(stream, this);

        public static WorldObjectPatch Deserialize(Stream stream)
            => Serializer.Deserialize<WorldObjectPatch>(stream);

        public override IEnumerator Apply()
        {
            Transform t = FindObjectByPath();

            t.TryGetComponent(out WorldObjectComponent cur_woc);

            for (int i = 0; i < components.Count; i++)
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

            UnityEngine.Object.Destroy(t.gameObject);

            yield return null;
        }
    }

    // -------------------------------------------------------------------

    [ProtoContract]
    [ProtoInclude(65537, typeof(WorldObjectInsertion))]
    [ProtoInclude(65538, typeof(WorldObjectPatch))]
    [ProtoInclude(65539, typeof(WorldObjectDeletion))]
    public abstract class WorldChange
    {
        [ProtoMember(1)]
        public List<Guid> path;

        protected Transform FindObjectByPath()
        {
            Transform t = GameObject.FindGameObjectWithTag("WorldObjectsRoot").transform;

            if (path == null) return t;

            foreach (Guid id in path)
            {
                Transform found = null;
                for (int i = 0; i < t.childCount; i++)
                {
                    if (!t.GetChild(i).TryGetComponent(out WorldObjectComponent woc)) continue;

                    if (id == woc.Id)
                    {
                        found = t.GetChild(i);
                        break;
                    }
                }

                if (!found)
                    throw new ArgumentException($"Unknown path element: {id}");

                t = found;
            }

            return t;
        }

        public abstract IEnumerator Apply();
    }

    // -------------------------------------------------------------------
    public static class GameObjectExtensions
    {
        public static WorldObject MakeWorldObject(this Transform t, bool includeChildren = true)
        {
            WorldObject wo = new();

            if (t.TryGetComponent(out WorldObjectComponent woc))
                wo.asset = woc.Asset;
            else return null; // filter out alien GameObjects

            wo.name = t.name;
            wo.components = woc.WOComponents;
            wo.id = woc.Id;

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
