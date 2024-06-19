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
using UnityEditor;

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
                woc.ReplaceComponent(components[i]);
        }

        public T GetWComponent<T>() where T : WOCBase
        {
            foreach(WOCBase w in components)
                if(w is T woc) return woc;
            return null;
        }

    }

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

        public static WorldObject MakeWorldObject(this GameObject go, bool includeChildren = true)
            => go.transform.MakeWorldObject(includeChildren);
    }
}
