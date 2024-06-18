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
    public struct WOQuaternion
    {
        [ProtoMember(1)]
        public float x;

        [ProtoMember(2)]
        public float y;

        [ProtoMember(3)]
        public float z;

        [ProtoMember(4)]
        public float w;

        public static implicit operator WOQuaternion(Quaternion q)
            => new() {x = q.x, y = q.y, z = q.z, w = q.w };

        public static implicit operator Quaternion(WOQuaternion q)
            => new() { x = q.x, y = q.y, z = q.z, w = q.w };
    }
}
