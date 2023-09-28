/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Newtonsoft.Json;
using System;
using System.IO;
using UnityEngine;

namespace Arteranos.Core
{

    public class ServerPermissionsJSON
    {
        // Allow avatars from a URL outside of the avatar generator's scope.
        public bool? CustomAvatars = false;

        // Allow flying
        public bool? Flying = false;

        // Allow connections of unverified users
        public bool? Guests = true;

        // CONTENT MODERATION / FILTERING
        // null allowed, and the user's filter could yield an inexact match, second only
        // to an exact one, like....
        //
        //  Setting     User        Priority
        //  false       false           1
        //  false       true            --
        //  false       null            1 (because the user says 'don't care')
        //  true        false           --
        //  true        true            1
        //  true        null            1 (because the user says 'don't care')
        //  null        false           2
        //  null        true            2
        //  null        null            2 (see below)
        //
        // as a side effect, server adminitrators get their servers a better ranking if they
        // put down a definite answer, in opposite being wishy-washy.
        //
        // ref. https://www.techdirt.com/2023/04/20/bluesky-plans-decentralized-composable-moderation/
        //      Defaults to Bluesky in the aforementioned website, with modifications

        // Explicit Sexual Images
        public bool? ExplicitNudes = null;

        // Other Nudity (eg. non-sexual or artistic)
        public bool? Nudity = true;

        // Sexually suggestive (does not include nudity)
        public bool? Suggestive = true;

        // Violence (Cartoon / "Clean" violence)
        public bool? Violence = null;

        // NEW
        //
        // Excessive Violence / Blood (Gore, self-harm, torture)
        public bool? ExcessiveViolence = false;

        // OMITTED
        //
        // (Political) Hate Groups - FALSE - Conflicts the law in many occasions
        // (eg. Germany, §1 GG, §130 StGB)
        //
        // Spam - FALSE - Self-explanatory
        //
        // Impersonation - FALSE - Self-explanatory

        /// <summary>
        /// Compute a match index for the server's settings against the user's filter preferences
        /// </summary>
        /// <param name="user">The user's server filter preferences</param>
        /// <returns>The match score, higher is better</returns>
        public int MatchIndex(ServerPermissionsJSON user)
        {
            int index = 0;

            bool usesGuest = SettingsManager.Client?.Me.Login.IsGuest ?? true;

            bool usesCustomAvatar = SettingsManager.Client?.Me.CurrentAvatar.IsCustom ?? true;

            // The 'Big Three' are true booleans - either true or false, no inbetweens.

            // Trying to use a guest login would be a disqualification criterium.
            if(usesGuest && !(Guests ?? true)) return 0;

            // Same as with custom avatars.
            if(usesCustomAvatar && !(CustomAvatars ?? true)) return 0;

            // Double weight for one of the 'Big Three'
            index += Flying.FuzzyEq(user.Flying) * 2;


            // Aggregate the matches of the permission settings against the user's
            // filter settings.
            index += ExplicitNudes.FuzzyEq(user.ExplicitNudes);

            index += Nudity.FuzzyEq(user.Nudity);

            index += Suggestive.FuzzyEq(user.Suggestive);

            index += Violence.FuzzyEq(user.Violence);

            index += ExcessiveViolence.FuzzyEq(user.ExcessiveViolence);

            return index;
        }

    }

    /// <summary>
    /// The static server configuration data.
    /// </summary>
    public class ServerSettingsJSON
    {
        [JsonIgnore]
        public static int DefaultMetadataPort = 9779;

        [JsonIgnore]
        public static string DefaultMetadataPath = "/metadata.json";

        [JsonIgnore]
        private bool includeCompleteKey = false;

        [JsonIgnore]
        private byte[] serverKey = null;

        [JsonIgnore]
        protected bool IncludeCompleteKey
        {
            get => includeCompleteKey;
            set => includeCompleteKey = value;
        }

        // The main server listen port.
        public int ServerPort = 9777;

        // The server metadata retrieval port.
        public int MetadataPort = DefaultMetadataPort;

        // Server listen address. Empty means allowing connections from anywhere.
        public string ListenAddress = string.Empty;

        // Allow viewing avatars in the server mode like in a spectator mode.
        public bool ShowAvatars = true;

        // The server nickname.
        public string Name = string.Empty;

        // The short server description.
        public string Description = string.Empty;

        // The server icon. PNG file bytes, at least 128x128, at most 512x512
        public byte[] Icon = new byte[] { };

        // The server's permissions
        public ServerPermissionsJSON Permissions = new();

        // The server's COMPLETE key
        public byte[] ServerKey
        {
            // Require explicit enabling the export of the whole key to prevent leaking
            // the key with the server settings
            get => includeCompleteKey ? serverKey : null;
            set => serverKey = value;
        }

        [JsonIgnore]
        public byte[] ServerPublicKey { get; set; } = null;


        public ServerSettingsJSON Strip()
        {
            return new ServerSettingsJSON()
            {
                ServerPort = ServerPort,
                MetadataPort = MetadataPort,
                ListenAddress = ListenAddress,
                ShowAvatars = ShowAvatars,
                Name = Name,
                Description = Description,
                Icon = new byte[0],         // Remove the icon to reduce the packet size
                Permissions = Permissions
            };
        }
    }

    public class ServerSettings : ServerSettingsJSON
    {
        public event Action<string> OnWorldURLChanged;

        // The world URL to load
        [JsonIgnore]
        private string m_WorldURL = string.Empty;

        [JsonIgnore]
        public string WorldURL
        {
            get => m_WorldURL;
            set
            {
                string old = m_WorldURL;
                m_WorldURL = value;
                if(old != m_WorldURL) OnWorldURLChanged?.Invoke(m_WorldURL);
            }
        }

        [JsonIgnore]
        private Crypto Crypto = null;

        public const string PATH_SERVER_SETTINGS = "ServerSettings.json";

        public void Save()
        {
            using Guard guard = new(
                () => IncludeCompleteKey = true,
                () => IncludeCompleteKey = false);

            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText($"{Application.persistentDataPath}/{PATH_SERVER_SETTINGS}", json);
            }
            catch(Exception e)
            {
                Debug.LogWarning($"Failed to save server settings: {e.Message}");
            }
        }

        public static ServerSettings Load()
        {
            ServerSettings ss;

            try
            {
                string json = File.ReadAllText($"{Application.persistentDataPath}/{PATH_SERVER_SETTINGS}");
                ss = JsonConvert.DeserializeObject<ServerSettings>(json);
            }
            catch(Exception e)
            {
                Debug.LogWarning($"Failed to load server settings: {e.Message}");
                ss = new();
            }

            using(Guard guard = new(
                () => ss.IncludeCompleteKey = true,
                () => ss.IncludeCompleteKey = false))
            {
                try
                {
                    if(ss.ServerKey == null)
                    {
                        ss.Crypto = new();
                        ss.ServerKey = ss.Crypto.Export(true);

                        ss.Save();
                    }
                    else
                    {
                        ss.Crypto = new(ss.ServerKey);
                    }
                }
                catch(Exception e)
                {
                    Debug.LogError($"Internal error while regenerating server key - regenerating...: {e.Message}");
                    ss.Crypto = new();
                    ss.ServerKey = ss.Crypto.Export(true);

                    ss.Save();
                }
            }

            ss.ServerPublicKey = ss.Crypto.PublicKey;
            return ss;
        }

        public void Decrypt<T>(CryptPacket p, out T payload) => Crypto.Decrypt(p, out payload);

        public void Sign(byte[] data, out byte[] signature) => Crypto.Sign(data, out signature);

        public static void TransmitMessage<T>(T data, byte[][] receiverPublicKeys, out CMSPacket packet)
            => SettingsManager.Server.Crypto.TransmitMessage(data, receiverPublicKeys, out packet);

        public static void TransmitMessage<T>(T data, byte[] receiverPublicKey, out CMSPacket packet)
            => SettingsManager.Server.Crypto.TransmitMessage(data, receiverPublicKey, out packet);

        public static void ReceiveMessage<T>(CMSPacket packet, ref byte[] expectedSignatureKey, out T data)
            => SettingsManager.Server.Crypto.ReceiveMessage(packet, ref expectedSignatureKey, out data);
    }
}