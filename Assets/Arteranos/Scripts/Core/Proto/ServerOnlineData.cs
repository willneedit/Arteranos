/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections.Generic;
using System.IO;
using Arteranos.Services;
using ProtoBuf;

namespace Arteranos.Core
{
    [ProtoContract]
    public partial class ServerOnlineData : PeerMessage
    {
        //[ProtoMember(1)]
        //public string CurrentWorldCid;

        //[ProtoMember(2)]
        //public string CurrentWorldName;

        [ProtoMember(3)]
        public string ServerDescriptionCid; // Just in case if we don't have the SD at all.

        [ProtoMember(4)]
        public List<byte[]> UserFingerprints = new();

        [ProtoMember(5)]
        public OnlineLevel OnlineLevel;

        [ProtoMember(6)]
        public string WorldCid;

        public override void Serialize(Stream stream)
            => Serializer.Serialize(stream, this);
    }
}