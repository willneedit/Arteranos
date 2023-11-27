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

namespace Arteranos.Core
{
    public class ServerInfo
    {
        private ServerPublicData? PublicData;
        private ServerOnlineData? OnlineData;

        public ServerInfo(string address, int port)
        {
            PublicData = SettingsManager.ServerCollection.Get(address, port);
            OnlineData = null;
        }

        public ServerInfo(string url)
        {
            Uri uri = new(url);

            PublicData = SettingsManager.ServerCollection.Get(uri.Host, uri.Port);
            OnlineData = null;
        }

        public async Task Update(int timeout = 1)
        {
            // Server's last sign of life is fresh, no need to poke it again.
            if (LastOnline <= DateTime.Now.AddMinutes(-2) || OnlineData == null)
                (PublicData, OnlineData) = await PublicData?.GetServerDataAsync(timeout);
        }

        public bool IsValid => PublicData != null;
        public bool IsOnline => OnlineData != null;
        public string Name => PublicData?.Name;
        public string Address => PublicData?.Address;
        public int Port => PublicData?.Port ?? 0;
        public string URL => $"http://{Address}:{Port}/";
        public byte[] Icon => OnlineData?.Icon;
        public ServerPermissions Permissions => PublicData?.Permissions;
        public DateTime LastOnline => PublicData?.LastOnline ?? DateTime.UnixEpoch;
        public string CurrentWorld => OnlineData?.CurrentWorld ?? string.Empty;
        public int UserCount => OnlineData?.UserPublicKeys.Count ?? 0;
        public int FriendCount
        {
            get
            {
                if (OnlineData == null) return 0;

                int friend = 0;
                IEnumerable<SocialListEntryJSON> friends = SettingsManager.Client.GetSocialList(null, arg => Social.SocialState.IsFriends(arg.State));

                foreach (SocialListEntryJSON entry in friends)
                    if (OnlineData.Value.UserPublicKeys.Contains(entry.UserID)) friend++;

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
    }

    /// <summary>
    /// Public server meta data with the connection data and the privileges
    /// </summary>
    public class ServerMetadataJSON
    {
        public ServerJSON Settings = null;
        [ASN1Tag(true)] public string CurrentWorld = null;
        public List<byte []> CurrentUsers = new();
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
