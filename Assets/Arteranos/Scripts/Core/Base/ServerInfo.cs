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

namespace Arteranos.Core
{
    public class ServerInfo
    {
        private ServerOnlineData OnlineData = null;
        private ServerDescription DescriptionStruct = null;

        public MultiHash PeerID { get; private set; } = null;

        private ServerInfo()
        {

        }

        public ServerInfo(MultiHash PeerID)
        {
            OnlineData = ServerOnlineData.DBLookup(PeerID.ToString());
            DescriptionStruct = ServerDescription.DBLookup(PeerID.ToString());
            this.PeerID = PeerID;
        }

        public async Task Update()
        {
            await Task.Run(() => {
                OnlineData = ServerOnlineData.DBLookup(PeerID.ToString());
                DescriptionStruct = ServerDescription.DBLookup(PeerID.ToString());
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
        public bool IsOnline => OnlineData != null;
        public string Name => DescriptionStruct?.Name;
        public string Description => DescriptionStruct?.Description ?? string.Empty;
        public string PrivacyTOSNotice => DescriptionStruct?.PrivacyTOSNotice;
        public byte[] Icon => DescriptionStruct?.Icon;
        public string[] AdminNames => DescriptionStruct?.AdminNames ?? new string[0];
        public int MDPort => DescriptionStruct?.MetadataPort ?? 0;
        public int ServerPort => DescriptionStruct?.ServerPort ?? 0;
        public string SPKDBKey => DescriptionStruct.PeerID;
        public ServerPermissions Permissions => DescriptionStruct?.Permissions ?? new();
        public DateTime LastUpdated => DescriptionStruct?.LastModified ?? DateTime.UnixEpoch;
        public DateTime LastOnline => OnlineData?.LastOnline ?? DateTime.UnixEpoch;
        public string CurrentWorldCid => OnlineData?.CurrentWorldCid;
        public string CurrentWorldName => (OnlineData?.CurrentWorldName) ?? "Nexus";
        public int UserCount => OnlineData?.UserFingerprints?.Count ?? 0;
        public int FriendCount
        {
            get
            {
                if (OnlineData?.UserFingerprints == null) return 0;

                int friend = 0;
                IEnumerable<SocialListEntryJSON> friends = SettingsManager.Client.GetSocialList(null, arg => Social.SocialState.IsFriends(arg.State));

                foreach (SocialListEntryJSON entry in friends)
                {
                    var q = from fpentry in OnlineData.UserFingerprints
                            where Convert.ToBase64String(fpentry) == 
                            CryptoHelpers.ToString(CryptoHelpers.FP_Base64, entry.UserID)
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
}
