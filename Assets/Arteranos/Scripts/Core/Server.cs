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

using DERSerializer;
using System.Collections.Generic;

namespace Arteranos.Core
{

    public class ServerPermissions : IEquatable<ServerPermissions>
    {
        // Allow avatars from a URL outside of the avatar generator's scope.
        [ASN1Tag( 1, true)] public bool? CustomAvatars = false;

        // Allow flying
        [ASN1Tag( 2, true)] public bool? Flying = false;

        // Allow connections of unverified users
        [ASN1Tag( 3, true)] public bool? Guests = true;

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
        [ASN1Tag(11, true)] public bool? ExplicitNudes = null;

        // Other Nudity (eg. non-sexual or artistic)
        [ASN1Tag(12, true)] public bool? Nudity = true;

        // Sexually suggestive (does not include nudity)
        [ASN1Tag(13, true)] public bool? Suggestive = true;

        // Violence (Cartoon / "Clean" violence)
        [ASN1Tag(14, true)] public bool? Violence = null;

        // NEW
        //
        // Excessive Violence / Blood (Gore, self-harm, torture)
        [ASN1Tag(15, true)] public bool? ExcessiveViolence = false;

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
        public (int, int) MatchRatio(ServerPermissions user)
        {
            static int possibleScore(bool? b1) => b1 == null ? 2 : 5;

            int index = 0;
            int possible = 10;

            bool usesGuest = SettingsManager.Client?.Me.Login.IsGuest ?? true;

            bool usesCustomAvatar = SettingsManager.Client?.Me.CurrentAvatar.IsCustom ?? true;

            // The 'Big Three' are true booleans - either true or false, no inbetweens.

            // Trying to use a guest login would be a disqualification criterium.
            if(usesGuest && !(Guests ?? true)) return (0, 100);

            // Same as with custom avatars.
            if(usesCustomAvatar && !(CustomAvatars ?? true)) return (0, 100);

            // Double weight for one of the 'Big Three'
            index += Flying.FuzzyEq(user.Flying) * 2;


            // Aggregate the matches of the permission settings against the user's
            // filter settings.
            possible += possibleScore(Nudity);
            index += Nudity.FuzzyEq(user.Nudity);

            possible += possibleScore(Suggestive);
            index += Suggestive.FuzzyEq(user.Suggestive);

            possible += possibleScore(Violence);
            index += Violence.FuzzyEq(user.Violence);

            possible += possibleScore(ExcessiveViolence);
            index += ExcessiveViolence.FuzzyEq(user.ExcessiveViolence);

            possible += possibleScore(ExplicitNudes);
            index += ExplicitNudes.FuzzyEq(user.ExplicitNudes);

            return (index, possible);
        }

        public string HumanReadableMI(ServerPermissions user)
        {
            (int index, int possible) = MatchRatio(user);
            float ratio = (float) index / (float) possible;

            string str = ratio switch
            {
               >= 1.0f => "perfect",
                > 0.8f => "very good",
                > 0.6f => "good",
                > 0.4f => "mediocre",
                > 0.2f => "poor",
                     _ => "very poor"
            };

            return $"{index} ({str})";
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ServerPermissions);
        }

        public bool Equals(ServerPermissions other)
        {
            return other is not null &&
                   CustomAvatars == other.CustomAvatars &&
                   Flying == other.Flying &&
                   Guests == other.Guests &&
                   ExplicitNudes == other.ExplicitNudes &&
                   Nudity == other.Nudity &&
                   Suggestive == other.Suggestive &&
                   Violence == other.Violence &&
                   ExcessiveViolence == other.ExcessiveViolence;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CustomAvatars, Flying, Guests, ExplicitNudes, Nudity, Suggestive, Violence, ExcessiveViolence);
        }

        public static bool operator ==(ServerPermissions left, ServerPermissions right)
        {
            return EqualityComparer<ServerPermissions>.Default.Equals(left, right);
        }

        public static bool operator !=(ServerPermissions left, ServerPermissions right)
        {
            return !(left == right);
        }
    }

    /// <summary>
    /// The static server configuration data.
    /// </summary>
    public class ServerJSON
    {
        [JsonIgnore]
        public static int DefaultMetadataPort = 9779;

        [JsonIgnore]
        public static string DefaultMetadataPath = "/metadata.asn1";

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

        // Public server. True means that the server's data can be spread around.
        public bool Public = true;

        // The server's permissions
        public ServerPermissions Permissions = new();

        // The server's COMPLETE key
        public byte[] ServerKey
        {
            // Require explicit enabling the export of the whole key to prevent leaking
            // the key with the server settings
            get => includeCompleteKey ? serverKey : null;
            set => serverKey = value;
        }

        [JsonIgnore]
        public byte[] ServerPublicKey = null;


        public ServerJSON Strip()
        {
            return new ServerJSON()
            {
                ServerPort = ServerPort,
                MetadataPort = MetadataPort,
                ListenAddress = ListenAddress,
                ShowAvatars = ShowAvatars,
                Name = Name,
                Description = Description,
                Icon = new byte[0],         // Remove the icon to reduce the packet size
                Permissions = Permissions,
                ServerPublicKey = ServerPublicKey
            };
        }
    }

    public class Server : ServerJSON
    {
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
                FileUtils.WriteTextConfig(PATH_SERVER_SETTINGS, json);
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
                    ss.ServerKey = null;
                }
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