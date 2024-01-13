/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using ProtoBuf;
using System;
using System.IO;


namespace Arteranos.Core
{
    [ProtoContract]
    public partial class WorldInfo
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

        [ProtoMember(10)]
        public DateTime Updated;

        public void Serialize(Stream stream)
            => Serializer.Serialize(stream, this);

        public static WorldInfo Deserialize(Stream stream)
            => Serializer.Deserialize<WorldInfo>(stream);

    }
}
