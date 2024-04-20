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
#if USE_SERVER_HELLO
    [ProtoInclude(65537, typeof(ServerHello))]
#endif
    [ProtoInclude(65538, typeof(ServerOnlineData))]
    public class PeerMessage
    {
        public virtual void Serialize(Stream stream)
            => Serializer.Serialize(stream, this);

        public static PeerMessage Deserialize(Stream stream)
            => Serializer.Deserialize<PeerMessage>(stream);
    }
}