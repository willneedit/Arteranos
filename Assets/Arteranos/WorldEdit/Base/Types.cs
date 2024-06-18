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

        [ProtoMember(7)]
        public List<WOCBase> components;  // Additional properties (like teleport marker, ...)

        [ProtoMember(8)]
        public List<WorldObject> children;  // grouped objects

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

        public IEnumerator Instantiate(Transform parent, Action<GameObject> callback = null)
        {
            IEnumerator LoadglTF(string GLTFObjectPath, GameObject LoadedObject)
            {
                // Need to be disabled until its completion, because the animation
                // wouldn't start
                LoadedObject.SetActive(false);

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

                LoadedObject.SetActive(true);
            }

            GameObject gob;

            // TODO: Implement kit item asset instantiation
            if (asset is WOPrimitive WOPR)                          // Pun intended :)
                gob = GameObject.CreatePrimitive(WOPR.primitive);
            else if(asset is WOglTF WOglTF)
            {
                gob = new GameObject("Unleaded glTF world object"); // :)
                yield return LoadglTF(WOglTF.glTFCid, gob);
            }
            else 
                gob = new GameObject("Empty or unsupported world object");

            // More complex constructs can be put as a child of an empty GameObject.

            gob.name = name;

            WorldObjectComponent woc = gob.AddComponent<WorldObjectComponent>();  
            woc.Asset = asset;
            woc.WOComponents = components;

            Transform t = gob.transform;
            t.SetParent(parent);

            // TODO: Assembling the GameObjects components from WOComponents
            foreach (WOCBase w in woc.WOComponents)
            {
                w.Awake(gob);
                w.CommitState();
            }

            foreach (WorldObject child in children)
                yield return child.Instantiate(t);

            yield return null;

            callback?.Invoke(gob);
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
        public static WorldObject MakeWorldObject(this Transform t)
        {
            WorldObject wo = new();

            if (t.TryGetComponent(out WorldObjectComponent asset))
                wo.asset = asset.Asset;
            else return null; // filter out alien GameObjects

            wo.components = asset.WOComponents;
            wo.name = t.name;

            for (int i = 0; i < t.childCount; ++i)
            {
                WorldObject item = MakeWorldObject(t.GetChild(i));
                if(item != null) wo.children.Add(item);
            }

            return wo;
        }

        public static WorldObject MakeWorldObject(this GameObject go)
            => go.transform.MakeWorldObject();
    }
}
