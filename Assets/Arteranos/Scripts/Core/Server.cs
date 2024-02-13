/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core.Cryptography;
using Ipfs.Core.Cryptography.Proto;
using Newtonsoft.Json;
using ProtoBuf;
using System;
using System.IO;
using UnityEngine;

namespace Arteranos.Core
{

    /// <summary>
    /// The static server configuration data.
    /// </summary>
    [ProtoContract]
    public class ServerJSON
    {
        // The main server listen port.
        [ProtoMember(1)]
        public int ServerPort = 9777;

        // The server metadata retrieval port.
        [ProtoMember(2)]
        public int MetadataPort = 9779;

        // The server nickname.
        [ProtoMember(3)]
        public string Name = string.Empty;

        // The short server description.
        [ProtoMember(4)]
        public string Description = string.Empty;

        // The server icon. PNG file bytes, at least 128x128, at most 512x512
        [ProtoMember(5)]
        public byte[] Icon = new byte[] { };

        // Public server. True means that the server's data can be spread around.
        [ProtoMember(6)]
        public bool Public = true;

        // The server's permissions
        [ProtoMember(7)]
        public ServerPermissions Permissions = new();

        [JsonIgnore]
        [ProtoMember(8)]
        public PublicKey ServerSignPublicKey = null;

        [JsonIgnore]
        [ProtoMember(9)]
        public PublicKey ServerAgrPublicKey = null;

        public ServerJSON Strip()
        {
            return new ServerJSON()
            {
                ServerPort = ServerPort,
                MetadataPort = MetadataPort,
                Name = Name,
                Description = Description,
                Public = Public,
                Icon = new byte[0],         // Remove the icon to reduce the packet size
                Permissions = Permissions,
                ServerSignPublicKey = ServerSignPublicKey,
                ServerAgrPublicKey = ServerAgrPublicKey
            };
        }
    }

    public class Server : ServerJSON
    {
        [JsonIgnore]
        public DateTime ConfigTimestamp { get; private set; }

        [JsonIgnore]
        private CryptoMessageHandler CMH = null;


        public const string PATH_SERVER_SETTINGS = "ServerSettings.json";

        public void Save()
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                FileUtils.WriteTextConfig(PATH_SERVER_SETTINGS, json);

                ConfigTimestamp = DateTime.Now;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to save server settings: {e.Message}");
            }
        }

        public static Server Load()
        {
            Server ss;

            try
            {
                string json = FileUtils.ReadTextConfig(PATH_SERVER_SETTINGS);
                ss = JsonConvert.DeserializeObject<Server>(json);

                if (FileUtils.NeedsFallback(PATH_SERVER_SETTINGS))
                {
                    Debug.LogWarning("Modifying server settings: Ports, Name, Server Key");
                    ss.ServerPort -= 100;
                    ss.MetadataPort -= 100;
                    ss.Name += " DS";
                }

                ss.ConfigTimestamp = FileUtils.ReadConfig(PATH_SERVER_SETTINGS, File.GetLastWriteTime);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load server settings: {e.Message}");
                ss = new() { ConfigTimestamp = DateTime.UnixEpoch };
            }

            return ss;
        }

        // Called back from the IPFS Service
        public void UpdateServerKey(SignKey serverKeyPair)
        {
            // Take the peer keypair as the (constant) signing key and its identification,
            // and generate a session-based key for (EC)DH key agreement.
            CMH = new(serverKeyPair);
            ServerSignPublicKey = CMH.SignPublicKey;
            ServerAgrPublicKey = CMH.AgreePublicKey;
        }

        public static void TransmitMessage(byte[] data, PublicKey receiver, out CMSPacket messageData)
            => SettingsManager.Server.CMH.TransmitMessage(data, receiver, out messageData);

        public static void ReceiveMessage(CMSPacket messageData, out byte[] data, out PublicKey signerPublicKey)
            => SettingsManager.Server.CMH.ReceiveMessage(messageData, out data, out signerPublicKey);
    }
}
