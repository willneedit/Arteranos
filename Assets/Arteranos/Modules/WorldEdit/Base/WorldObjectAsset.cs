/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using ProtoBuf;
using System;
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
    public class WOglTF : WorldObjectAsset, IEquatable<WOglTF>
    {
        [ProtoMember(1)]
        public string glTFCid;  // 1. Single glTF file

        public override HashSet<AssetReference> GetAssetReferences() => new() { new("glTF", glTFCid) };

        public override bool Equals(object obj)
        {
            return Equals(obj as WOglTF);
        }

        public bool Equals(WOglTF other)
        {
            return other is not null &&
                   glTFCid == other.glTFCid;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(glTFCid);
        }

        public static bool operator ==(WOglTF left, WOglTF right)
        {
            return EqualityComparer<WOglTF>.Default.Equals(left, right);
        }

        public static bool operator !=(WOglTF left, WOglTF right)
        {
            return !(left == right);
        }
    }

    [ProtoContract]
    public class WOKitItem : WorldObjectAsset, IEquatable<WOKitItem>
    {
        [ProtoMember(1)]
        public string kitCid;       // 2a. Kit (collection of objects) file

        [ProtoMember(2)]
        public Guid kitItemName;    // 2b. File, referring to an object im AssetBundle

        public override HashSet<AssetReference> GetAssetReferences() => new() { new("Kit", kitCid) };

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

    [ProtoContract]
    public class WOPrimitive : WorldObjectAsset, IEquatable<WOPrimitive>
    {
        [ProtoMember(1)]
        public PrimitiveType primitive;

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

    [ProtoContract]
    [ProtoInclude(65537, typeof(WOglTF))]
    [ProtoInclude(65538, typeof(WOKitItem))]
    [ProtoInclude(65539, typeof(WOPrimitive))]
    public abstract class WorldObjectAsset : IWorldObjectAsset, IHasAssetReferences
    {
        public abstract HashSet<AssetReference> GetAssetReferences();
    }
}
