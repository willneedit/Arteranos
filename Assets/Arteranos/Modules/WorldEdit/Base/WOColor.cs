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
    public struct WOColor
    {
        [ProtoMember(1)]
        public float r;

        [ProtoMember(2)]
        public float g;

        [ProtoMember(3)]
        public float b;

        [ProtoMember(4)]
        public float a;

        public static implicit operator WOColor(Color c)
            => new() { r = c.r, g = c.g, b = c.b, a = c.a };

        public static implicit operator Color(WOColor c)
            => new() { r = c.r, g = c.g, b = c.b, a = c.a };
    }
}
