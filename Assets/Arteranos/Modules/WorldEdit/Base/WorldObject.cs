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
using AssetBundle = Arteranos.Core.Managed.AssetBundle;
using Arteranos.WorldEdit.Components;
using Arteranos.Core.Managed;

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
            static IEnumerator LoadglTF(WOglTF WOglTF, GameObject LoadedObject)
            {
                using CancellationTokenSource cts = new(60000);
                using IPFSGLTFObject obj = new(WOglTF.glTFCid, cts.Token)
                {
                    RootObject = LoadedObject,
                    InitActive = false
                };

                yield return obj.GameObject.WaitFor();

                // Add a box collider with with the approximated bounds.
                Bounds? b = obj.Bounds;
                if(b.HasValue)
                {
                    BoxCollider bc = LoadedObject.AddComponent<BoxCollider>();
                    bc.center = b.Value.center;
                    bc.size = b.Value.size;
                }

                LoadedObject.name = $"glTF {WOglTF.glTFCid}";
            }
            
            static IEnumerator LoadKit(WOKitItem kitItem, GameObject LoadedObject)
            {
                LoadedObject.name = $"kit {kitItem.kitCid}, Item {kitItem.kitItemName}";

                AsyncLazy<AssetBundle> KitAB = G.WorldEditorData.LoadKitAssetBundle(kitItem.kitCid);

                yield return KitAB.WaitFor();

                GameObject kit_blueprint = ((UnityEngine.AssetBundle) KitAB.Result).LoadAsset<GameObject>($"Assets/KitRoot/{kitItem.kitItemName}.prefab");

                // Really scrubbing the asset bundle's blueprints' components.
                ScrubComponents(kit_blueprint);

                // And, reset its root transform.
                // Kit builders who REALLY use offsets have to use empties.
                kit_blueprint.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                // kit_blueprint.transform.localScale = Vector3.one;

                UnityEngine.Object.Instantiate(kit_blueprint, LoadedObject.transform);
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

        private static void ScrubComponents(GameObject kit_blueprint)
        {
            kit_blueprint.SetActive(false);
            ScrubComponents(kit_blueprint.transform);
            kit_blueprint.SetActive(true);
        }

        private static void ScrubComponents(Transform kit_blueprint)
        {
            static bool IsValidComponent(Component component)
            {
                // Missing script or engine package - not OK.
                if(component == null) return false;

                // Not a script - OK.
                if (component is not MonoBehaviour) return true;

                string assname = component.GetType().Assembly.GetName().Name;

                // Userspace namespace - OK
                if(assname == "Arteranos.User") return true;

                // Everything else - not OK.
                return false;
            }

            Component[] components = kit_blueprint.GetComponents<Component>();
            foreach (Component component in components)
            {
                if (IsValidComponent(component)) continue;

                UnityEngine.Object.DestroyImmediate(component, true);
            }

            for (int i = 0; i < kit_blueprint.transform.childCount; i++)
                ScrubComponents(kit_blueprint.transform.GetChild(i));
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
