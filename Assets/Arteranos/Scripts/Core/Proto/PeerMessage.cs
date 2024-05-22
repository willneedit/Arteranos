/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.IO;
using ProtoBuf;

namespace Arteranos.Core
{
    [ProtoContract]
    [ProtoInclude(65538, typeof(ServerOnlineData))]
    [ProtoInclude(65539, typeof(ServerDescriptionLink))]
    public class PeerMessage
    {
        public virtual void Serialize(Stream stream)
            => Serializer.Serialize(stream, this);

        public static PeerMessage Deserialize(Stream stream)
            => Serializer.Deserialize<PeerMessage>(stream);
    }
}