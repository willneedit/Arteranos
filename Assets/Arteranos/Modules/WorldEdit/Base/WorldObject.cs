﻿/*
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
using GLTFast;
using Mono.Cecil.Cil;
using System.Threading.Tasks;

namespace Arteranos.WorldEdit
{
    [ProtoContract]
    public class WorldObject : IHasAssetReferences
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

            components = new();

            children = new();
        }

        public void Serialize(Stream stream)
            => Serializer.Serialize(stream, this);

        public static WorldObject Deserialize(Stream stream)
            => Serializer.Deserialize<WorldObject>(stream);

        public IEnumerator Instantiate(Transform parent, Action<GameObject> callback = null)
        {
            static async Task<AssetBundleCreateRequest> LoadAssetBundle(string path)
            {
                MemoryStream ms = await G.IPFSService.ReadIntoMS(
                    $"{path}/{Utils.GetArchitectureDirName()}/kit");

                return AssetBundle.LoadFromStreamAsync(ms);

            }

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
                    GameObjectBoundsInstantiator instantiator = new(gltf, LoadedObject.transform);

                    yield return Asyncs.Async2Coroutine(
                        gltf.InstantiateMainSceneAsync(instantiator));

                    // Add a box collider with with the approximated bounds.
                    Bounds? b = instantiator.CalculateBounds();
                    if(b.HasValue)
                    {
                        BoxCollider bc = LoadedObject.AddComponent<BoxCollider>();
                        bc.center = b.Value.center;
                        bc.size = b.Value.size;
                    }
                }

                LoadedObject.name = $"glTF {WOglTF.glTFCid}";
            }
            
            static IEnumerator LoadKit(WOKitItem kitItem, GameObject LoadedObject)
            {
                LoadedObject.name = $"kit {kitItem.kitCid}, Item {kitItem.kitItemName}";

                if(!G.WorldEditorData.TryGetKitAssetBundle(kitItem.kitCid, out AssetBundle ab))
                {
                    AssetBundleCreateRequest abc = null;

                    yield return Asyncs.Async2Coroutine(
                        LoadAssetBundle(kitItem.kitCid),
                        _result => abc = _result);

                    yield return new WaitUntil(() => abc.isDone);

                    ab = abc.assetBundle;

                    G.WorldEditorData.AddKitAssetBundle(kitItem.kitCid, ab);
                }

                GameObject ob = ab.LoadAsset<GameObject>($"Assets/KitRoot/{kitItem.kitItemName}.prefab");

                UnityEngine.Object.Instantiate(ob, LoadedObject.transform);
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

        /// <summary>
        /// Gets all assets we'd need this World Object to function.
        /// </summary>
        /// <returns>Asset references, from the asset itself and the object's components</returns>
        public HashSet<AssetReference> GetAssetReferences()
        {
            HashSet<AssetReference> references = new();

            if(asset != null)
                references.UnionWith(asset.GetAssetReferences());

            foreach(WOCBase w in components)
                references.UnionWith(w.GetAssetReferences()); 

            return references;
        }

        public HashSet<AssetReference> GetAssetReferences(bool recursive)
        {
            HashSet<AssetReference> references = GetAssetReferences();
            if(recursive)
            {
                foreach (WorldObject child in children)
                    references.UnionWith(child.GetAssetReferences(recursive));
            }

            return references;
        }
    }
}