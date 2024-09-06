/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.IO;
using Arteranos.Core.Cryptography;
using Ipfs;
using Mirror;
using ProtoBuf;

namespace Arteranos.Core
{
    public struct CTSPacketEnvelope : NetworkMessage
    {
        // public UserID receiver; // Unneeded. Either client to server or server to client
        public CMSPacket CTSPayload; // Encrypted for the receiver
    }

    [ProtoContract]
    [ProtoInclude(65537, typeof(CTSPUpdateUserState))]
    [ProtoInclude(65538, typeof(CTSPWorldChangeAnnouncement))]
    [ProtoInclude(65539, typeof(STCUserInfo))]
    [ProtoInclude(65540, typeof(CTSMessage))]
    [ProtoInclude(65541, typeof(CTSServerConfig))]
    [ProtoInclude(65542, typeof(CTSWorldObjectChange))]
    public class CTSPacket
    {
        [ProtoMember(1)]
        public UserID invoker; // Who caused it, or null by the server itself

        public virtual byte[] Serialize()
        {
            using MemoryStream ms = new();
            Serializer.Serialize(ms, this);
            return ms.ToArray();
        }

        public static CTSPacket Deserialize(byte[] data)
            => Serializer.Deserialize<CTSPacket>(new MemoryStream(data));
    }

    // C: Update the user's privilege, or kick/ban user
    // S: Tell user that he's been sactioned
    [ProtoContract]
    public class CTSPUpdateUserState : CTSPacket
    {
        [ProtoMember(1)]
        public UserID receiver; // Even if it's not set in the Server User State.

        [ProtoMember(2)]
        public ServerUserState State;

        [ProtoMember(3)]
        public bool toDisconnect;
    }

    // C: Ask server for the notable(!) user base
    // S: Tell invoker the list, one at a time
    [ProtoContract]
    public class STCUserInfo : CTSPacket
    {
        [ProtoMember(1)]
        public UserID UserID;

        [ProtoMember(2)]
        public ServerUserState State;
    }

    // C: Invoke a world change
    // S to all: Pull the users into the new world
    // S to invoker: Cid == null) Tell the invoker that the world loading failed 
    [ProtoContract]
    public class CTSPWorldChangeAnnouncement : CTSPacket
    {
        [ProtoMember(1)]
        public Cid WorldRootCid;
    }

    // S: Dialog message
    [ProtoContract]
    public class CTSMessage : CTSPacket
    {
        [ProtoMember(1)]
        public string message;
    }

    // S: Server configuration update
    // C: Server configuration info
    [ProtoContract]
    public class CTSServerConfig : CTSPacket
    {
        [ProtoMember(1)]
        public ServerJSON config;
    }

    // C: World builder sends a change request about a world object
    // S: Server broadcasts the world object change to everyone (including the world builder)
    [ProtoContract]
    public class CTSWorldObjectChange : CTSPacket
    {
        [ProtoMember(1)]
        public byte[] changerequest; // Arteranos.WorldEdit.WorldChange, serialized
    }
}