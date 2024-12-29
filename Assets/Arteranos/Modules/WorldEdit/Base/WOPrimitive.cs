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
        public PrimitiveType primitive;

        public override GameObject Create()
        {
            GameObject gobbo = GameObject.CreatePrimitive(primitive);
            gobbo.TryGetComponent(out Renderer renderer);
            renderer.material =  WBP.I.Objects.DefaultWEMaterial;
            gobbo.SetActive(false);

            return gobbo;
        }

        public override IEnumerator CreateCoroutine(GameObject gobbo)
        {
            yield return null;
        }

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
