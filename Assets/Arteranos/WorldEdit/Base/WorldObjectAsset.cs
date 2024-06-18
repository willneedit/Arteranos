/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using ProtoBuf;
using UnityEngine;

namespace Arteranos.WorldEdit
{
    [ProtoContract]
    public class WOglTF : WorldObjectAsset
    {
        [ProtoMember(1)]
        public string glTFCid;  // 1. Single glTF file
    }

    [ProtoContract]
    public class WOKitItem : WorldObjectAsset
    {
        [ProtoMember(1)]
        public string kitCid;   // 2a. Kit (collection of objects) file

        [ProtoMember(2)]
        public string kitName;  // 2b. File, referring to an object im AssetBundle
    }

    [ProtoContract]
    public class WOPrimitive : WorldObjectAsset
    {
        [ProtoMember(1)]
        public PrimitiveType primitive;
    }

    [ProtoContract]
    [ProtoInclude(65537, typeof(WOglTF))]
    [ProtoInclude(65538, typeof(WOKitItem))]
    [ProtoInclude(65539, typeof(WOPrimitive))]
    public class WorldObjectAsset
    {
    }
}
