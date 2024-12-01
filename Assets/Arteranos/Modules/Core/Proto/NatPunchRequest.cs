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
        public string relayIP;  // The relay to contact to, if it's using relayed

        [ProtoMember(2)]
        public int relayPort;

        [ProtoMember(3)]
        public string token;    // The token, provided by the peer which wants to contact us

        [ProtoMember(4)]
        public string serverPeerID;

        [ProtoMember(5)]
        public string clientIP; // The client who wants to connect to the firewalled server, if it's relayless

        [ProtoMember(6)]
        public int clientPort;

        public string ToPeerID { get => serverPeerID; set => serverPeerID = value; }

        public override void Serialize(Stream stream)
            => Serializer.Serialize(stream, this);
    }
}