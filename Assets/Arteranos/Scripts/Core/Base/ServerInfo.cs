/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Ipfs;
using Arteranos.Core.Cryptography;

namespace Arteranos.Core
{
    public class ServerInfo
    {
        private ServerOnlineData OnlineData = null;
        private ServerDescription DescriptionStruct = null;

        public WorldInfo WorldInfo { get; internal set; } = null;
        public MultiHash PeerID { get; private set; } = null;

        private ServerInfo()
        {

        }

        public ServerInfo(MultiHash PeerID)
        {
            OnlineData = ServerOnlineData.DBLookup(PeerID.ToString());
            DescriptionStruct = ServerDescription.DBLookup(PeerID.ToString());

            if(OnlineData != null) 
                WorldInfo = WorldInfo.Retrieve(OnlineData.WorldCid);

            this.PeerID = PeerID;
        }

        public async Task Update()
        {
            await Task.Run(() => {
                OnlineData = ServerOnlineData.DBLookup(PeerID.ToString());
                DescriptionStruct = ServerDescription.DBLookup(PeerID.ToString());

                if (OnlineData != null)
                    WorldInfo = WorldInfo.Retrieve(OnlineData.WorldCid);
            });
        }

        public void Delete()
        {
            ServerOnlineData.DBDelete(PeerID.ToString());
            ServerDescription.DBDelete(PeerID.ToString());
        }
        public static IEnumerable<ServerInfo> Dump(DateTime cutoff)
        {
            foreach(ServerDescription sd in ServerDescription.DBList())
            {
                if (sd.LastModified < cutoff) continue;

                yield return new ServerInfo()
                {
                    OnlineData = ServerOnlineData.DBLookup(sd.PeerID),
                    DescriptionStruct = sd,
                    PeerID = sd.PeerID,
                };
            }
        }

        public bool IsValid => DescriptionStruct != null;
        public bool SeenOnline => OnlineData != null;
        public bool IsOnline => OnlineData != null && OnlineData.LastOnline > (DateTime.Now - TimeSpan.FromMinutes(5));
        public string Name => DescriptionStruct?.Name;
        public string Description => DescriptionStruct?.Description ?? string.Empty;
        public string PrivacyTOSNotice => DescriptionStruct?.PrivacyTOSNotice;
        public byte[] Icon => DescriptionStruct?.Icon;
        public string[] AdminNames => DescriptionStruct?.AdminNames ?? new string[0];
        public int MDPort => DescriptionStruct?.MetadataPort ?? 0;
        public int ServerPort => DescriptionStruct?.ServerPort ?? 0;
        public string SPKDBKey => DescriptionStruct.PeerID;
        public ServerPermissions Permissions => DescriptionStruct?.Permissions ?? new();
        public DateTime LastUpdated => DescriptionStruct?.LastModified ?? DateTime.MinValue;
        public DateTime LastOnline => OnlineData?.LastOnline ?? DateTime.MinValue;
        public string CurrentWorldCid => WorldInfo?.WorldCid ?? null;
        public string CurrentWorldName => (WorldInfo?.WorldName) ?? "Nexus";
        public int UserCount => OnlineData?.UserFingerprints?.Count ?? 0;
        public int FriendCount
        {
            get
            {
                if (OnlineData?.UserFingerprints == null) return 0;

                int friend = 0;
                IEnumerable<KeyValuePair<UserID, ulong>> friends = SettingsManager.Client.GetSocialList(null, arg => Social.SocialState.IsFriends(arg.Value));

                foreach (KeyValuePair<UserID, ulong> entry in friends)
                {
                    var q = from fpentry in OnlineData.UserFingerprints
                            where Convert.ToBase64String(fpentry) == 
                            CryptoHelpers.ToString(CryptoHelpers.FP_Base64, entry.Key)
                            select fpentry;

                    if (q.Any()) friend++;
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

                return Hashes.SHA256(PrivacyTOSNotice);
            }
        }

        public bool UsesCustomTOS
        {
            get 
            {
                if (PrivacyTOSNotice == null) return false; 
                return !PrivacyTOSNoticeHash.SequenceEqual(Hashes.SHA256(SettingsManager.DefaultTOStext));
            }
        }
    }
}
