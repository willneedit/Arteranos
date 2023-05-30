/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;
using System;

using Mirror;
using System.ComponentModel;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace Arteranos.ExtensionMethods
{
    using Arteranos.NetworkTypes;

    public static class ExtendTransform
    {
        /// <summary>
        /// Finds the transform in the hierarchy tree by name, including searching the
        /// entire subtree below.
        /// </summary>
        /// <param name="t">The transform to begin searching</param>
        /// <param name="name">The transform's name to search for</param>
        /// <returns>The first found transform, otherwise null</returns>
        public static Transform FindRecursive(this Transform t, string name)
        {
            if(t.name == name) return t;

            for(int i = 0, c = t.childCount; i<c; i++)
            {
                Transform res = FindRecursive(t.GetChild(i), name);
                if(res != null) return res;
            }

            return null;
        }
    }

    public static class ExtendNetworkGuid
    {
        
        public static NetworkGuid ToNetworkGuid(this Guid id)
        {
            NetworkGuid networkId = new()
            {
                FirstHalf = BitConverter.ToUInt64(id.ToByteArray(), 0),
                SecondHalf = BitConverter.ToUInt64(id.ToByteArray(), 0)
            };
            return networkId;
        }

        public static Guid ToGuid(this NetworkGuid networkId)
        {
            byte[] bytes = new byte[16];
            Buffer.BlockCopy(BitConverter.GetBytes(networkId.FirstHalf), 0, bytes, 0, 8);
            Buffer.BlockCopy(BitConverter.GetBytes(networkId.SecondHalf), 0, bytes, 8, 8);
            return new Guid(bytes);
        }

        public static void WriteNetworkGuid(this NetworkWriter writer, NetworkGuid value)
        {
            writer.WriteULong(value.FirstHalf);
            writer.WriteULong(value.SecondHalf);
        }

        public static NetworkGuid ReadNetworkGuid(this NetworkReader reader)
        {
            NetworkGuid res = new()
            {
                FirstHalf = reader.ReadULong(),
                SecondHalf = reader.ReadULong()
            };
            return res;
        }

    }
}

namespace Arteranos.NetworkTypes
{
    public class NetworkGuid 
    {
        public ulong FirstHalf;
        public ulong SecondHalf;

    }
}

namespace Arteranos.Core
{
    public static class Extensions
    {
        /// <summary>
        /// Returns a relevance index for the comparison.
        /// </summary>
        /// <param name="setting">The server settings</param>
        /// <param name="user">The user's search filter</param>
        /// <returns>5 for an exact determinate match, 1 for an inexact match, 0 for a mismatch</returns>
        public static int FuzzyEq(this bool? setting, bool? user)
        {
            if(setting == null) return 1;

            return !setting != user ? 5 : 0;
        }
    }

    public static class TransformExtensions
    {
        // public static CancellationTokenSource ctx = null;

        public static async Task LerpTransform(this Transform transform,
            Transform targetTransform, float duration, CancellationToken token)
        {
            // ctx?.Cancel();
            // ctx = new CancellationTokenSource();

            float time = 0f;
            Vector3 startPosition = transform.localPosition;
            Quaternion startRotation = transform.localRotation;
            Vector3 startScale = transform.localScale;

            while(time < duration && !token.IsCancellationRequested)
            {
                float t = time / duration;

                t = 0.5f - (float) Mathf.Cos(t * Mathf.PI) * 0.5f;
                transform.localPosition = Vector3.Lerp(startPosition, targetTransform.localPosition, t);
                transform.localRotation = Quaternion.Lerp(startRotation, targetTransform.localRotation, t);
                transform.localScale = Vector3.Lerp(startScale, targetTransform.localScale, t);
                time += Time.deltaTime;
                await Task.Yield();
            }

            if(!token.IsCancellationRequested)
            {
                transform.localPosition = targetTransform.localPosition;
                transform.localRotation = targetTransform.localRotation;
                transform.localScale = targetTransform.localScale;
            }
        }
    }

    public class Utils
    {
        /// <summary>
        /// Allows to tack on a Description attribute to enum values, e.g. a display name.
        /// </summary>
        /// <param name="enumVal">The particular value of the enum set</param>
        /// <returns>The string in the value's description, null if there isn't</returns>
        public static string GetEnumDescription(Enum enumVal)
        {
            MemberInfo[] memInfo = enumVal.GetType().GetMember(enumVal.ToString());
            DescriptionAttribute attribute = CustomAttributeExtensions.GetCustomAttribute<DescriptionAttribute>(memInfo[0]);
            return attribute?.Description;
        }

        /// <summary>
        /// Generate a directory of a hashed URL suitable for a cache directory tree.
        /// </summary>
        /// <param name="url">The URL</param>
        /// <returns>the two directory levels, without a root path</returns>
        public static string GetURLHash(string url)
        {
            Hash128 hash = new();
            byte[] bytes = Encoding.UTF8.GetBytes(url);
            hash.Append(bytes);
            string hashstr = hash.ToString();

            string hashed = $"{hashstr[0..2]}/{hashstr[2..]}";
            return hashed;
        }

        /// <summary>
        /// Simulate a RC circuit (a capacitor and resistor) to measure the capacitor's charge,
        /// used in for example a VU meter. 
        /// </summary>
        /// <param name="value">Current input voltage</param>
        /// <param name="charge">The resulting charge</param>
        /// <param name="kCharge">The charging factor</param>
        /// <param name="kDischarge">The discharging factor</param>
        public static void CalcVU(float value, ref float charge, float kCharge = 0.1f, float kDischarge = 0.05f)
        {
            value = Mathf.Abs(value);

            if(value > charge)
                charge = (charge * (1 - kCharge)) + (value * kCharge);
            else
                charge *= (1 - kDischarge);
        }

        /// <summary>
        /// Fout = 10^(Q/20) * Fin
        /// </summary>
        /// <param name="dBvalue"></param>
        /// <returns>Ife plain factor.</returns>
        public static float LoudnessToFactor(float dBvalue) => MathF.Pow(10.0f, dBvalue / 10.0f);

        public static Uri ProcessUriString(string urilike,
                        string scheme = null,
                        string host = null,
                        int? port = null,
                        string path = null,
                        string query = null,
                        string fragment = null
)
        {
            urilike = urilike.Trim();

            if(!urilike.Contains("://"))
                urilike = "unknown://" + urilike;

            Uri uri = new(urilike);

            if(uri.Port >= 0)
                port = uri.Port;

            if(port == null)
                throw new ArgumentNullException("No port");

            string sb = string.IsNullOrEmpty(host ?? uri.Host)
                ? $"{scheme ?? uri.Scheme}://"
                : string.IsNullOrEmpty(uri.UserInfo)
                    ? $"{scheme ?? uri.Scheme}://{host ?? uri.Host}:{port}"
                    : $"{scheme ?? uri.Scheme}://{uri.UserInfo}@{host ?? uri.Host}:{port}";

            sb += uri.AbsolutePath == "/"
                ? path ?? "/"
                : uri.AbsolutePath;

            sb += string.IsNullOrEmpty(uri.Query)
                ? query ?? string.Empty
                : uri.Query;

            sb += string.IsNullOrEmpty(uri.Fragment)
                ? fragment ?? string.Empty
                : uri.Fragment;

            return new(sb);
        }
    }

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

        // The main server listen port.
        public int ServerPort = 9777;

        // The voice server listen port.
        public int VoicePort = 9778;

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

        public ServerSettingsJSON Strip()
        {
            ServerSettingsJSON newSS = new()
            {
                ServerPort = ServerPort,
                VoicePort = VoicePort,
                MetadataPort = MetadataPort,
                ListenAddress = ListenAddress,
                ShowAvatars = ShowAvatars,
                Name = Name,
                Description = Description,
                Icon = new byte[0],         // Remove the icon to reduce the packet size
                Permissions = Permissions
            };
            return newSS;
        }
    }


    /// <summary>
    /// Public server meta data with the connection data and the privileges
    /// </summary>
    public class ServerMetadataJSON
    {
        public ServerSettingsJSON Settings = null;
        public string CurrentWorld = null;
        public List<string> CurrentUsers = new();
    }

    public class WorldMetaData
    {
        public const string PATH_METADATA_DEFAULTS = "MetadataDefaults.json";

        public string WorldName = "Unnamed World";
        public string Author = "Anonymous";
        public DateTime Created = DateTime.MinValue;
        public DateTime Updated = DateTime.MinValue;

        public void SaveDefaults()
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText($"{Application.persistentDataPath}/{PATH_METADATA_DEFAULTS}", json);
            }
            catch(System.Exception ex)
            {
                Debug.LogWarning($"Failed to save the metadata defaults: {ex.Message}");
            }
        }

        public static WorldMetaData LoadDefaults()
        {
            WorldMetaData mdj;
            try
            {
                string json = File.ReadAllText($"{Application.persistentDataPath}/{PATH_METADATA_DEFAULTS}");
                mdj = JsonConvert.DeserializeObject<WorldMetaData>(json);
            }
            catch(System.Exception ex)
            {
                Debug.LogWarning($"Failed to load the metadata defaults: {ex.Message}");
                mdj = new();
            }

            return mdj;
        }

        public string Serialize() => JsonConvert.SerializeObject(this, Formatting.Indented);

        public static WorldMetaData Deserialize(string json) => JsonConvert.DeserializeObject<WorldMetaData>(json);
    }
}
