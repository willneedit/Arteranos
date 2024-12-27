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
    public readonly struct AssetReference : IEquatable<AssetReference>
    {
        public readonly string type;
        public readonly string cid;

        public AssetReference(string type, string cid)
        {
            this.type = type;
            this.cid = cid;
        }

        public override bool Equals(object obj)
        {
            return obj is AssetReference reference && Equals(reference);
        }

        public bool Equals(AssetReference other)
        {
            return type == other.type &&
                   cid == other.cid;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(type, cid);
        }

        public static bool operator ==(AssetReference left, AssetReference right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AssetReference left, AssetReference right)
        {
            return !(left == right);
        }
    }

    public interface IHasAssetReferences
    {
        HashSet<AssetReference> GetAssetReferences();
    }

    [ProtoContract]
    [ProtoInclude(65537, typeof(WOglTF))]
    [ProtoInclude(65538, typeof(WOKitItem))]
    [ProtoInclude(65539, typeof(WOPrimitive))]
    public abstract class WorldObjectAsset : IWorldObjectAsset, IHasAssetReferences
    {
        public abstract GameObject Create();
        public abstract IEnumerator CreateCoroutine(GameObject gobbo);
        public abstract HashSet<AssetReference> GetAssetReferences();
    }
}
