/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core.Cryptography;
using Newtonsoft.Json;
using System;
using System.IO;
using UnityEngine;

namespace Arteranos.Core
{

    /// <summary>
    /// The static server configuration data.
    /// </summary>
    public class ServerJSON
    {
        // The main server listen port.
        public int ServerPort = 9777;

        // The server metadata retrieval port.
        public int MetadataPort = 9779;

        // The server nickname.
        public string Name = string.Empty;

        // The short server description.
        public string Description = string.Empty;

        // The server icon. PNG file bytes, at least 128x128, at most 512x512
        public byte[] Icon = new byte[] { };

        // Public server. True means that the server's data can be spread around.
        public bool Public = true;

        // The server's permissions
        public ServerPermissions Permissions = new();

        [JsonIgnore]
        public byte[] ServerPublicKey = null;

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
                ServerPublicKey = ServerPublicKey
            };
        }
    }

    public class Server : ServerJSON
    {
        [JsonIgnore]
        public DateTime ConfigTimestamp { get; private set; }

        [JsonIgnore]
        private CryptoMessageHandler CMH = null;

        [JsonIgnore]
        private Crypto Crypto = null;

        public const string PATH_SERVER_SETTINGS = "ServerSettings.json";

        public void Save()
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                FileUtils.WriteTextConfig(PATH_SERVER_SETTINGS, json);

                ConfigTimestamp = DateTime.Now;
            }
            catch(Exception e)
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

                if(FileUtils.NeedsFallback(PATH_SERVER_SETTINGS))
                {
                    Debug.LogWarning("Modifying server settings: Ports, Name, Server Key");
                    ss.ServerPort -= 100;
                    ss.MetadataPort -= 100;
                    ss.Name += " DS";
                }

                ss.ConfigTimestamp = FileUtils.ReadConfig(PATH_SERVER_SETTINGS, File.GetLastWriteTime);
            }
            catch(Exception e)
            {
                Debug.LogWarning($"Failed to load server settings: {e.Message}");
                ss = new() { ConfigTimestamp = DateTime.UnixEpoch };
            }

            return ss;
        }

        // Called back from the IPFS Service
        public void UpdateServerKey(SignKey serverKeyPair)
        {
            serverKeyPair.ExportPublicKey(out ServerPublicKey);
            CMH = new(serverKeyPair);
        }

        public void Decrypt<T>(CryptPacket p, out T payload) => Crypto.Decrypt(p, out payload);

        public void Sign(byte[] data, out byte[] signature) => CMH.Sign(data, out signature);

        public static void TransmitMessage<T>(T data, byte[] receiverPublicKey, out CMSPacket packet)
            => SettingsManager.Server.Crypto.TransmitMessage(data, receiverPublicKey, out packet);

        public static void ReceiveMessage<T>(CMSPacket packet, ref byte[] expectedSignatureKey, out T data)
            => SettingsManager.Server.Crypto.ReceiveMessage(packet, ref expectedSignatureKey, out data);
    }
}