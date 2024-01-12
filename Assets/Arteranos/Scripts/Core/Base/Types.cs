/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;

using DERSerializer;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.Linq;
using Ipfs;

namespace Arteranos.Core
{
    public static partial class Utils
    {
        public async static Task<(bool, T)> WebRetrieve<T>(string url, string pathPart, string patternServerDescription, int expirySeconds, int timeout = 1, bool keepExpired = false)
        {
            UriBuilder builder = new(url) { Path = pathPart };

            byte[] data = await CachedDownloadWebData(builder.ToString(), url, patternServerDescription, expirySeconds, timeout, keepExpired);
            if (data == null) return (false, default(T));

            try
            {
                return (true, Serializer.Deserialize<T>(data));
            }
            catch
            {
                InvalidateWebData(builder.ToString(), url, patternServerDescription);
                return (false, default(T));
            }
        }

        public static void WebDelete(string url, string pathPart, string patternServerDescription)
        {
            UriBuilder builder = new(url) { Path = pathPart };
            InvalidateWebData(builder.ToString(), url, patternServerDescription);
        }


        public async static Task WebEmit<T>(T payload, HttpListenerResponse response)
        {
            byte[] data = Serializer.Serialize(payload);
            response.ContentType = "application/octet-stream";
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = data.LongLength;
            response.StatusCode = (int)HttpStatusCode.OK;

            await response.OutputStream.WriteAsync(data, 0, data.Length);
            response.Close();
        }

    }

    public class ServerInfo
    {
        private _ServerOnlineData OnlineData = null;
        private ServerDescription DescriptionStruct = null;

        public MultiHash PeerID { get; private set; } = null;

        private ServerInfo()
        {

        }

        public ServerInfo(MultiHash PeerID)
        {
            OnlineData = null; // TODO
            DescriptionStruct = ServerDescription.DBLookup(PeerID.ToString());
            this.PeerID = PeerID;
        }

        public async Task Update()
        {
            await Task.Run(() => {
                OnlineData = null; // TODO
                DescriptionStruct = ServerDescription.DBLookup(PeerID.ToString());
            });
        }

        public void Delete()
        {
            // TODO
            ServerDescription.DBDelete(PeerID.ToString());
        }
        public static IEnumerable<ServerInfo> Dump(DateTime cutoff)
        {
            foreach(ServerDescription sd in ServerDescription.DBList())
            {
                if (sd.LastModified < cutoff) continue;

                yield return new ServerInfo()
                {
                    OnlineData = null, // TODO
                    DescriptionStruct = sd,
                    PeerID = sd.PeerID,
                };
            }
        }

        public bool IsValid => DescriptionStruct != null;
        public bool IsOnline => OnlineData != null;
        public string Name => DescriptionStruct?.Name;
        public string Description => DescriptionStruct?.Description ?? string.Empty;
        public string PrivacyTOSNotice => DescriptionStruct?.PrivacyTOSNotice;
        public byte[] Icon => DescriptionStruct?.Icon;
        public string[] AdminNames => DescriptionStruct?.AdminNames ?? new string[0];
        // public string Address => PublicData?.Address;
        public int MDPort => DescriptionStruct?.MetadataPort ?? 0;
        public int ServerPort => DescriptionStruct?.ServerPort ?? 0;
        //public string URL => $"http://{Address}:{MDPort}/";
        public string SPKDBKey => DescriptionStruct.PeerID;
        public ServerPermissions Permissions => DescriptionStruct?.Permissions ?? new();
        public DateTime LastUpdated => DescriptionStruct?.LastModified ?? DateTime.UnixEpoch;
        public DateTime LastOnline => OnlineData?.LastOnline ?? DateTime.UnixEpoch;
        public string CurrentWorldCid => OnlineData?.CurrentWorldCid;
        public string CurrentWorldName => (OnlineData?.CurrentWorldName != null) ? OnlineData?.CurrentWorldName : "Nexus";
        public int UserCount => OnlineData?.UserFingerprints.Length ?? 0;
        public int FriendCount
        {
            get
            {
                if (OnlineData == null) return 0;

                int friend = 0;
                IEnumerable<SocialListEntryJSON> friends = SettingsManager.Client.GetSocialList(null, arg => Social.SocialState.IsFriends(arg.State));

                foreach (SocialListEntryJSON entry in friends)
                {
                    byte[] fingerprint = CryptoHelpers.GetFingerprint(entry.UserID);
                    if (OnlineData.UserFingerprints.Contains(fingerprint)) friend++;
                }

                return friend;
            }
        }
        public int MatchScore
        {
            get
            {
                (int ms, int _) = Permissions.MatchRatio(SettingsManager.Client.ContentFilterPreferences);
                return ms + FriendCount * 3;
            }
        }

        public byte[] PrivacyTOSNoticeHash
        {
            get
            {
                if (PrivacyTOSNotice == null) return null;

                return Crypto.SHA256(PrivacyTOSNotice);
            }
        }

        public bool UsesCustomTOS
        {
            get 
            {
                if (PrivacyTOSNotice == null) return false; 
                return !PrivacyTOSNoticeHash.SequenceEqual(Crypto.SHA256(Utils.LoadDefaultTOS()));
            }
        }
    }

    public struct WorldInfo
    {
        public WorldMetaData metaData;
        
        [ASN1Tag(1, true)] 
        public byte[] signature;

        [ASN1Tag(2, true)]
        public byte[] screenshotPNG;

        public DateTime updated;
    }

    public class WorldMetaData
    {
        public const string PATH_METADATA_DEFAULTS = "MetadataDefaults.json";

        public string WorldName = "Unnamed World";
        public string WorldDescription = string.Empty;
        public UserID AuthorID = null;
        public ServerPermissions ContentRating = null;
        public bool RequiresPassword = false;
        public DateTime Created = DateTime.MinValue;

        public void SaveDefaults()
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText($"{Application.persistentDataPath}/{PATH_METADATA_DEFAULTS}", json);
            }
            catch(Exception ex)
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
            catch(Exception ex)
            {
                Debug.LogWarning($"Failed to load the metadata defaults: {ex.Message}");
                mdj = new();
            }

            return mdj;
        }

        public string Serialize() => JsonConvert.SerializeObject(this, Formatting.Indented);

        public static WorldMetaData Deserialize(string json) => JsonConvert.DeserializeObject<WorldMetaData>(json);
    }

    /// <summary>
    /// Suitable for locks/unlocks to properly deallocate resources when the control flow
    /// leaves the scope, be it regular or by an exception.
    /// 
    /// Credits go for C++ :)
    /// Best use with
    ///             using(Guard guard = new(allocate, release)) { ... }
    /// </summary>
    public class Guard : IDisposable
    {
        private readonly Action disengage;

        private bool _disposedValue;

        public Guard(Action engage, Action disengage)
        {
            this.disengage = disengage;
            try
            {
                engage();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        ~Guard() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if(_disposedValue) return;

            //if(disposing)
            //{
            //    // Needed? Dispose managed state (managed objects).
            //}

            try
            {
                disengage();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            _disposedValue = true;
        }
    }
}
