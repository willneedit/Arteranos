/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;


namespace Arteranos.Core
{
    [ProtoContract]
    public partial class WorldInfo : IEquatable<WorldInfo>
    {
        [ProtoMember(1)]
        public string WorldCid;

        [ProtoMember(2)]
        public string WorldName;

        [ProtoMember(3)]
        public string WorldDescription;

        [ProtoMember(4)]
        public string AuthorNickname;

        [ProtoMember(5)]
        public byte[] AuthorPublicKey;

        [ProtoMember(6)]
        public ServerPermissions ContentRating;

        [ProtoMember(7)]
        public byte[] Signature;

        [ProtoMember(8)]
        public byte[] ScreenshotPNG;

        [ProtoMember(9)]
        public DateTime Created;

        // [ProtoMember(10)] -- remains locally
        public DateTime Updated;

        public void Serialize(Stream stream)
            => Serializer.Serialize(stream, this);

        public static WorldInfo Deserialize(Stream stream)
            => Serializer.Deserialize<WorldInfo>(stream);

        public override bool Equals(object obj)
        {
            return Equals(obj as WorldInfo);
        }

        public bool Equals(WorldInfo other)
        {
            return other is not null &&
                   WorldCid == other.WorldCid &&
                   WorldName == other.WorldName &&
                   WorldDescription == other.WorldDescription &&
                   AuthorNickname == other.AuthorNickname &&
                   EqualityComparer<ServerPermissions>.Default.Equals(ContentRating, other.ContentRating) &&
                   Created == other.Created;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(WorldCid, WorldName, WorldDescription, AuthorNickname, ContentRating, Created);
        }

        public static bool operator ==(WorldInfo left, WorldInfo right)
        {
            return EqualityComparer<WorldInfo>.Default.Equals(left, right);
        }

        public static bool operator !=(WorldInfo left, WorldInfo right)
        {
            return !(left == right);
        }
    }
}
