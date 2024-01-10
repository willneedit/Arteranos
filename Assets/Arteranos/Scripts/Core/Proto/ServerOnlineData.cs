/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.IO;
using ProtoBuf;

namespace Arteranos.Core
{
    [ProtoContract]
    public partial class _ServerOnlineData : PeerMessage
    {
        [ProtoMember(1)]
        public string CurrentWorldCID;

        [ProtoMember(2)]
        public string CurrentWorldName;

        [ProtoMember(3)]
        public string ServerDescriptionCID; // Just in case if we don't have the SD at all.

        [ProtoMember(4)]
        public byte[][] UserFingerprints;

        public void Serialize(Stream stream)
            => Serializer.Serialize(stream, this);
    }
}