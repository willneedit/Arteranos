/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using System.Collections.Generic;
using ProtoBuf;

using UnityEngine;
using System.IO;
using System;
using Arteranos.WorldEdit.Components;

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

        public WorldObject(PrimitiveTypeEx primitive)
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
                    gobbo = asset.Create();
                    yield return asset.CreateCoroutine(gobbo);

                    gobbo.transform.position = new Vector3(0, -9000, 0);
                    G.WorldEditorData.AddBlueprint(asset, gobbo);

                    // Blueprints need to be inactive at its completion
                    // because it's half-finished.
                    Debug.Assert(gobbo.activeInHierarchy == false);
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
