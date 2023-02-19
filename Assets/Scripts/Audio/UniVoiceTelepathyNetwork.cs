/*
 * Copyright (c) 2023, willneedit
 * Copyright (c) 2018 Vatsal Ambastha
 * 
 * Based on https://github.com/adrenak/univoice-telepathy-network ,
 * heavily modified using the Mirror Networking package
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections.Generic;

using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine;
using System.Linq;

using Adrenak.UniVoice;
using Mirror;
using Telepathy;

namespace Arteranos.Audio {
    public class UniVoiceTelepathyNetwork : MonoBehaviour, IChatroomNetwork {
        enum PacketType : byte
        {
            Invalid,
            ClientInit,
            ClientJoined,
            ClientLeft,
            AudioSegment
        }

        // HOSTING EVENTS
        public event Action OnCreatedChatroom;
        public event Action<Exception> OnChatroomCreationFailed;
        public event Action OnClosedChatroom;

        // JOINING EVENTS
        public event Action<short> OnJoinedChatroom;
        public event Action<Exception> OnChatroomJoinFailed;
        public event Action OnLeftChatroom;

        // PEER EVENTS
        public event Action<short> OnPeerJoinedChatroom;
        public event Action<short> OnPeerLeftChatroom;

        // AUDIO EVENTS
        public event Action<short, ChatroomAudioSegment> OnAudioReceived;
        public event Action<short, ChatroomAudioSegment> OnAudioSent;

        public short OwnID { get; private set; } = -1;

        public List<short> PeerIDs { get; private set; } = new List<short>();

        Server server;
        Client client;
        int port;

        [Obsolete("Use UniVoiceTelepathyNetwork.New static method instead of new keyword", true)]
        public UniVoiceTelepathyNetwork() { }

        public static UniVoiceTelepathyNetwork New(int port) {
            GameObject go = new("UniVoiceTelepathyNetwork");
            UniVoiceTelepathyNetwork cted = go.AddComponent<UniVoiceTelepathyNetwork>();
            DontDestroyOnLoad(go);
            cted.port = port;
            cted.server = new Server(32 * 1024);
            cted.server.OnConnected += cted.OnConnected_Server;
            cted.server.OnDisconnected += cted.OnDisconnected_Server;
            cted.server.OnData += cted.OnData_Server;

            cted.client = new Client(32 * 1024);
            cted.client.OnData += cted.OnData_Client;
            cted.client.OnDisconnected += cted.OnDisconnected_Client;
            return cted;
        }

        void Update() {
            server?.Tick(100);
            client?.Tick(100);
        }

        void OnDestroy() {
            Dispose();
        }

        public void Dispose() {
            client.Disconnect();
            server.Stop();
        }

        void OnData_Client(ArraySegment<byte> data) {
            try {
                NetworkReader packet = new(data.Array);
                PacketType tag = (PacketType) packet.ReadByte();
                
                switch (tag) {
                    case PacketType.ClientInit:
                        OwnID = packet.ReadShort();
                        OnJoinedChatroom?.Invoke(OwnID);
                        PeerIDs = packet.ReadArray<short>().ToList();
                        foreach (short peer in PeerIDs)
                            OnPeerJoinedChatroom?.Invoke(peer);
                        break;
                    case PacketType.ClientJoined:
                        short joinedID = packet.ReadShort();
                        if (!PeerIDs.Contains(joinedID))
                            PeerIDs.Add(joinedID);
                        OnPeerJoinedChatroom?.Invoke(joinedID);
                        break;
                    case PacketType.ClientLeft:
                        short leftID = packet.ReadShort();
                        if (PeerIDs.Contains(leftID))
                            PeerIDs.Remove(leftID);
                        OnPeerLeftChatroom?.Invoke(leftID);
                        break;
                    case PacketType.AudioSegment:
                        short sender = packet.ReadShort();
                        short recepient = packet.ReadShort();
                        if(recepient == OwnID) {
                            ChatroomAudioSegment segment = FromByteArray<ChatroomAudioSegment>(packet.ReadArray<byte>());
                            OnAudioReceived?.Invoke(sender, segment);
                        }
                        break;
                    default:
                        Debug.LogError($"Invalid packet: {nameof(PacketType)} == {tag}");
                        break;
                }
            }
            catch { }
        }

        void OnDisconnected_Client() {
            if (OwnID == -1) {
                OnChatroomJoinFailed?.Invoke(new Exception("Could not join chatroom"));
                return;
            }
            PeerIDs.Clear();
            OwnID = -1;
            OnLeftChatroom?.Invoke();
        }

        void OnConnected_Server(int obj) {
            short id = (short)obj;

            if(id != 0) {
                if (!PeerIDs.Contains(id))
                    PeerIDs.Add(id);
                foreach (short peer in PeerIDs) {
                    // Let the new client know its ID
                    if (peer == id) {
                        var peersForNewClient = PeerIDs
                            .Where(x => x != peer)
                            .ToList();
                        peersForNewClient.Add(0);

                        NetworkWriter newClientPacket = new();
                        newClientPacket.WriteByte((byte) PacketType.ClientInit);
                        newClientPacket.WriteShort(id);
                        newClientPacket.WriteArray(peersForNewClient.ToArray());
                        server.Send(peer, newClientPacket.ToArraySegment());
                    }
                    // Let other clients know a new peer has joined
                    else {
                        NetworkWriter oldClientsPacket = new();
                        oldClientsPacket.WriteByte((byte) PacketType.ClientJoined);
                        oldClientsPacket.WriteShort(id);
                        server.Send(peer, oldClientsPacket.ToArraySegment());
                    }
                }
                OnPeerJoinedChatroom?.Invoke(id);
            }
        }

        void OnData_Server(int sender, ArraySegment<byte> data) {
            NetworkReader packet = new(data);
            PacketType tag = (PacketType) packet.ReadByte();

            if (tag == PacketType.AudioSegment) {
                short audioSender = packet.ReadShort();
                short recipient = packet.ReadShort();
                byte[] segmentBytes = packet.ReadArray<byte>();

                if (recipient == OwnID) {
                    ChatroomAudioSegment segment = FromByteArray<ChatroomAudioSegment>(segmentBytes);
                    OnAudioReceived?.Invoke(audioSender, segment);
                }
                else if (PeerIDs.Contains(recipient))
                    server.Send(recipient, new ArraySegment<byte>(segmentBytes));
            }
        }

        void OnDisconnected_Server(int id) {
            short id_short = (short)id;
            if(id != 0) {
                if (PeerIDs.Contains(id_short))
                    PeerIDs.Remove(id_short);
                foreach (short peer in PeerIDs) {

                    NetworkWriter packet = new();
                    packet.WriteByte((byte) PacketType.ClientLeft);
                    packet.WriteShort(id_short);
                    server.Send(peer, packet.ToArraySegment());
                }
                OnPeerLeftChatroom?.Invoke(id_short);
            }
        }

        public void HostChatroom(object data = null) {
            if (!server.Active) {
                if(server.Start(port)) {
                    OwnID = 0;
                    PeerIDs.Clear();
                    OnCreatedChatroom?.Invoke();
                }
            }
            else
                Debug.LogWarning("HostChatroom failed. Already hosting a chatroom. Close and host again.");
        }

        /// <summary>
        /// Closes the chatroom, if hosting
        /// </summary>
        /// <param name="data"></param>
        public void CloseChatroom(object data = null) {
            if (server != null && server.Active) {
                server.Stop();
                PeerIDs.Clear();
                if (OwnID == -1) {
                    OnChatroomCreationFailed?.Invoke(new Exception("Could not create chatroom"));
                    return;
                }
                OwnID = -1;
                OnClosedChatroom?.Invoke();
            }
            else
                Debug.LogWarning("CloseChatroom failed. Not hosting a chatroom currently");
        }

        /// <summary>
        /// Joins a chatroom. If passed null, will connect to "localhost"
        /// </summary>
        /// <param name="data"></param>
        public void JoinChatroom(object data = null) {
            if(client.Connected || client.Connecting)
                client.Disconnect();
            if (client.Connected) {
                Debug.LogWarning("JoinChatroom failed. Already connected to a chatroom. Leave and join again.");
                return;
            }
            if (client.Connecting) {
                Debug.LogWarning("JoinChatroom failed. Currently attempting to connect to a chatroom. Leave and join again");
                return;
            }
            string ip = (data != null) ? (string) data : "localhost";
            client.Connect(ip, port);
        }

        /// <summary>
        /// Leave a chatroom. Data passed is not used
        /// </summary>
        /// <param name="data"></param>
        public void LeaveChatroom(object data = null) {
            if (client.Connected || client.Connecting)
                client.Disconnect();
            else
                Debug.LogWarning("LeaveChatroom failed. Currently connected to any chatroom");
        }

        /// <summary>
        /// Send a <see cref="ChatroomAudioSegment"/> to a peer
        /// This method is used internally by <see cref="ChatroomAgent"/>
        /// invoke it manually at your own risk!
        /// </summary>
        /// <param name="peerID"></param>
        /// <param name="data"></param>
        public void SendAudioSegment(short peerID, ChatroomAudioSegment data) {
            if (!server.Active && !client.Connected) return;

            var packet = new NetworkWriter();
            packet.WriteByte((byte) PacketType.AudioSegment);
            // Sender
            packet.WriteShort(OwnID);
            // Recipient
            packet.WriteShort(peerID);
            packet.WriteArray(ToByteArray(data));

            if (server.Active) 
                server.Send(peerID, packet.ToArraySegment());
            else if (client.Connected) 
                client.Send(packet.ToArraySegment());

            OnAudioSent?.Invoke(peerID, data);
        }

        public byte[] ToByteArray<T>(T obj) {
            if (obj == null)
                return null;
            BinaryFormatter bf = new();
            using MemoryStream ms = new();
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }

        public T FromByteArray<T>(byte[] data) {
            if (data == null)
                return default;
            BinaryFormatter bf = new();
            using MemoryStream ms = new(data);
            object obj = bf.Deserialize(ms);
            return (T) obj;
        }
    }
}