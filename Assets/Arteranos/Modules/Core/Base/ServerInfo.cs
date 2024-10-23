/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Ipfs;
using Arteranos.Core.Cryptography;
using System.Net;

namespace Arteranos.Core
{
    public class ServerInfo
    {
        private ServerOnlineData OnlineData = null;
        private ServerDescription DescriptionStruct = null;

        public MultiHash PeerID { get; private set; } = null;

        public string CurrentWorldCid => OnlineData?.CurrentWorldCid;
        public string CurrentWorldName => OnlineData?.CurrentWorldName;

        private ServerInfo()
        {

        }

        public ServerInfo(MultiHash PeerID)
        {
            this.PeerID = PeerID;

            OnlineData = ServerOnlineData.DBLookup(PeerID.ToString());
            DescriptionStruct = ServerDescription.DBLookup(PeerID.ToString());
        }

        public void Delete()
        {
            ServerOnlineData.DBDelete(PeerID.ToString());
            ServerDescription.DBDelete(PeerID.ToString());
        }
        public static IEnumerable<ServerInfo> Dump()
        {
            foreach(ServerDescription sd in ServerDescription.DBList())
            {
                yield return new ServerInfo()
                {
                    OnlineData = ServerOnlineData.DBLookup(sd.PeerID),
                    DescriptionStruct = sd,
                    PeerID = sd.PeerID,
                };
            }
        }

        public bool IsValid => DescriptionStruct;
        public bool SeenOnline => OnlineData != null;
        public bool IsOnline => OnlineData != null && OnlineData.LastOnline > (DateTime.UtcNow - TimeSpan.FromMinutes(5)) && OnlineData.OnlineLevel != Services.OnlineLevel.Offline;
        public string Name => DescriptionStruct?.Name;
        public string Description => DescriptionStruct?.Description ?? string.Empty;
        public string PrivacyTOSNotice => DescriptionStruct?.PrivacyTOSNotice;
        public Cid ServerIcon => DescriptionStruct?.ServerIcon;
        public string[] AdminNames => DescriptionStruct?.AdminNames ?? new string[0];
        public IEnumerable<IPAddress> IPAddresses => from entry in OnlineData.IPAddresses select IPAddress.Parse(entry);
        public int ServerPort => DescriptionStruct?.ServerPort ?? 0;
        public string SPKDBKey => DescriptionStruct.PeerID;
        public ServerPermissions Permissions => DescriptionStruct?.Permissions ?? new();
        public DateTime LastUpdated => DescriptionStruct?.LastModified ?? DateTime.MinValue;
        public DateTime LastOnline
        {
            get
            {
                DateTime online = OnlineData?.LastOnline ?? DateTime.MinValue;
                DateTime data = DescriptionStruct?.LastSeen ?? DateTime.MinValue;

                return online > data ? online : data;
            }
        }

        public int UserCount => OnlineData?.UserFingerprints?.Count ?? 0;
        public int FriendCount
        {
            get
            {
                if (OnlineData?.UserFingerprints == null) return 0;

                int friend = 0;
                IEnumerable<KeyValuePair<UserID, UserSocialEntryJSON>> friends = G.Client.GetSocialList(null, arg => Social.SocialState.IsFriends(arg.Value.State));

                foreach (KeyValuePair<UserID, UserSocialEntryJSON> entry in friends)
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
                (int ms, int _) = Permissions.MatchRatio(G.Client.ContentFilterPreferences);
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
