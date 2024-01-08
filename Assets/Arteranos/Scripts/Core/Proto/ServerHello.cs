/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.IO;
using Ipfs;
using ProtoBuf;

namespace Arteranos.Core
{
    [ProtoContract]
    public partial class ServerHello
    {
        [ProtoMember(1)]
        public string ServerDescriptionCid;

        [ProtoMember(2)]
        public DateTime LastModified;

        public void Serialize(Stream stream) 
            => Serializer.Serialize(stream, this);

        public void Serialize(out byte[] bytes)
        {
            using MemoryStream ms = new();
            Serialize(ms);
            ms.Position = 0;
            bytes = ms.ToArray();
        }

        public static ServerHello Deserialize(Stream stream) 
            => Serializer.Deserialize<ServerHello>(stream);
    }
}