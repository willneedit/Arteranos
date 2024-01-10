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
    //public enum PeerMessageType
    //{
    //    ServerHello     = 0x48454C4F, // 'HELO' - Sending the server's general description
    //    ImOkay          = 0x494D4F4B, // 'IMOK' - Sending the server's user list and current world CID
    //    AreYouThere     = 0x4159543F, // 'AYT?' - Prompts (PeerID) with an ImOkay response
    //}

    [ProtoContract]
    [ProtoInclude(65537, typeof(ServerHello))]
    [ProtoInclude(65538, typeof(_ServerOnlineData))]
    public class PeerMessage
    {
        public static PeerMessage Deserialize(Stream stream)
            => Serializer.Deserialize<PeerMessage>(stream);
    }
}