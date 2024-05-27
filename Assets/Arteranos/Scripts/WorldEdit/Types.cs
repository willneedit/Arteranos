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
        [ProtoMember(1)]
        public string name;
    }

    [ProtoContract]
    public class WorldDecoration
    {
        [ProtoMember(1)]
        public WorldInfoNetwork info;

        [ProtoMember(2)]
        public List<WorldObject> objects;
    }

    [ProtoContract]
    public class WorldObject
    {
        [ProtoMember(1)]
        public WorldObjectAsset asset;      // see above

        [ProtoMember(3)]
        public WOVector3 position;            // local to parent

        [ProtoMember(4)]
        public WOQuaternion rotation;         // local to parent

        [ProtoMember(5)]
        public WOVector3 scale;               // local to parent

        [ProtoMember(6)]
        public WOColor color;

        [ProtoMember(7)]
        public List<WOComponent> components;  // Additional properties (like teleport marker, ...)

        [ProtoMember(8)]
        public List<WorldObject> children;  // grouped objects

        public WorldObject()
        {
            Init();
        }

        public WorldObject(PrimitiveType primitive)
        {
            Init();
            asset = new WOPrimitive { primitive = primitive, name = string.Empty };
        }

        private void Init()
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            scale = Vector3.one;
            components = new();
            children = new();
        }
    }

    [ProtoContract]
    public class WOComponent
    {
        // Extensible with subclassing done by ProtoInclude
    }
}
