/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AssetBundle = Arteranos.Core.Managed.AssetBundle;

namespace Arteranos.WorldEdit
{
    [ProtoContract]
    public class WOKitItem : WorldObjectAsset, IEquatable<WOKitItem>
    {
        [ProtoMember(1)]
        public string kitCid;       // 2a. Kit (collection of objects) file

        [ProtoMember(2)]
        public Guid kitItemName;    // 2b. File, referring to an object in AssetBundle

        public override HashSet<AssetReference> GetAssetReferences() => new() { new("Kit", kitCid) };

        public override GameObject Create()
        {
            GameObject gobbo = new GameObject("Unleaded kit world object");
            gobbo.SetActive(false);

            return gobbo;
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
                if (component == null) return false;

                // Not a script - OK.
                if (component is not MonoBehaviour) return true;

                string assname = component.GetType().Assembly.GetName().Name;

                // Userspace namespace - OK
                if (assname == "Arteranos.User") return true;

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

        public override IEnumerator CreateCoroutine(GameObject LoadedObject)
        {
            LoadedObject.name = $"kit {kitCid}, Item {kitItemName}";

            AsyncLazy<AssetBundle> KitAB = G.WorldEditorData.LoadKitAssetBundle(kitCid);

            yield return KitAB.WaitFor();

            GameObject kit_blueprint = ((UnityEngine.AssetBundle)KitAB.Result).LoadAsset<GameObject>($"Assets/KitRoot/{kitItemName}.prefab");

            // Really scrubbing the asset bundle's blueprints' components.
            ScrubComponents(kit_blueprint);

            // And, reset its root transform.
            // Kit builders who REALLY use offsets have to use empties.
            kit_blueprint.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            // kit_blueprint.transform.localScale = Vector3.one;

            UnityEngine.Object.Instantiate(kit_blueprint, LoadedObject.transform);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as WOKitItem);
        }

        public bool Equals(WOKitItem other)
        {
            return other is not null &&
                   kitCid == other.kitCid &&
                   kitItemName == other.kitItemName;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(kitCid, kitItemName);
        }

        public static bool operator ==(WOKitItem left, WOKitItem right)
        {
            return EqualityComparer<WOKitItem>.Default.Equals(left, right);
        }

        public static bool operator !=(WOKitItem left, WOKitItem right)
        {
            return !(left == right);
        }
    }
}
