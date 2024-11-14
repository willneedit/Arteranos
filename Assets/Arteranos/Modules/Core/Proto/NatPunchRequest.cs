/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections.Generic;
using System.IO;
using ProtoBuf;

namespace Arteranos.Core
{
    /// <summary>
    /// Both this and the other peer needs to contact a relay for the NAT punching process.
    /// The other one have (most probably) already contacted the relay, and this message
    /// have been arrived via IPFS.
    /// Once both peers contact the relay, it replies to both peers to commence the second stage.
    /// </summary>
    [ProtoContract]
    public class NatPunchRequestData : PeerMessage, IDirectedPeerMessage
    {
        [ProtoMember(1)]
        public string relayIP;  // The relay to contact to

        [ProtoMember(2)]
        public int relayPort;

        [ProtoMember(3)]
        public string token;    // The token, provided by the peer which wants to contact us

        [ProtoMember(4)]
        public string toPeerID;

        public string ToPeerID { get => toPeerID; set => toPeerID = value; }

        public override void Serialize(Stream stream)
            => Serializer.Serialize(stream, this);
    }
}