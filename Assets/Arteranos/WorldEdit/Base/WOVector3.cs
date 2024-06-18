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
    // Protobuf serializable version
    [ProtoContract]
    public struct WOVector3
    {
        [ProtoMember(1)]
        public float x;

        [ProtoMember(2)]
        public float y;

        [ProtoMember(3)]
        public float z;

        public static implicit operator WOVector3(Vector3 v)
            => new() { x = v.x, y = v.y, z = v.z };

        public static implicit operator Vector3(WOVector3 v) 
            => new() { x = v.x, y = v.y, z = v.z };
    }
}
