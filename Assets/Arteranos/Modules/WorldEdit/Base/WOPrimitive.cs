/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arteranos.WorldEdit
{
    [ProtoContract]
    public class WOPrimitive : WorldObjectAsset, IEquatable<WOPrimitive>
    {
        [ProtoMember(1)]
        public PrimitiveTypeEx primitive;

        public override GameObject Create()
        {
            GameObject gobbo = CreatePrimitiveEx(primitive);
            Renderer renderer = gobbo.GetComponentInChildren<Renderer>();
            renderer.material =  WBP.I.Objects.DefaultWEMaterial;
            gobbo.SetActive(false);

            return gobbo;
        }

        public override IEnumerator CreateCoroutine(GameObject gobbo)
        {
            yield return null;
        }

        public static GameObject CreatePrimitiveEx(PrimitiveTypeEx primitive)
        {
            if (primitive < PrimitiveTypeEx._SimpleEnd)
                return GameObject.CreatePrimitive((PrimitiveType)primitive);

            GameObject go = UnityEngine.Object.Instantiate(WBP.I.GetPrimitiveEx(primitive));
            MeshCollider coll = go.GetComponentInChildren<MeshCollider>();
            coll.convex = true;

            return go;
        }

        // ---------------------------------------------------------------

        public override bool Equals(object obj)
        {
            return Equals(obj as WOPrimitive);
        }

        public bool Equals(WOPrimitive other)
        {
            return other is not null &&
                   primitive == other.primitive;
        }

        public override HashSet<AssetReference> GetAssetReferences() => new();

        public override int GetHashCode()
        {
            return HashCode.Combine(primitive);
        }

        public static bool operator ==(WOPrimitive left, WOPrimitive right)
        {
            return EqualityComparer<WOPrimitive>.Default.Equals(left, right);
        }

        public static bool operator !=(WOPrimitive left, WOPrimitive right)
        {
            return !(left == right);
        }
    }
}
