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
#if USE_SERVER_HELLO
    [ProtoContract]
    public partial class ServerHello : PeerMessage
    {
        [ProtoContract]
        public partial class SDLink
        {
            [ProtoMember(1)]
            public string ServerDescriptionCid;

            [ProtoMember(2)]
            public DateTime LastModified;

            [ProtoMember(4)]
            public string PeerID;
        }

        [ProtoMember(3)]
        public List<SDLink> Links;

        public override void Serialize(Stream stream) 
            => Serializer.Serialize(stream, this);
    }
#endif
}