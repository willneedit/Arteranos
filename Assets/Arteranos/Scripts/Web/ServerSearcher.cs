/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using System;
using System.Threading.Tasks;

using System.Collections.Generic;
using UnityEngine;
using System.Threading;

namespace Arteranos.Web
{
    internal class ServerInfo
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
        public string Name => PublicData?.Name;
        public string Address => PublicData?.Address;
        public int Port => PublicData?.Port ?? 0;
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

    internal class ServerSearcherContext : Context
    {
        public List<ServerInfo> serverInfos = null;
        public string desiredWorldURL = null;
        public string resultServerURL = null;
    }

    internal class FetchServersOp : IAsyncOperation<Context>
    {
        public int Timeout { get; set; }
        public float Weight { get; set; } = 1.0f;
        public string Caption { get; set; } = "Fetching servers";
        public Action<float> ProgressChanged { get; set; }

        public async Task<Context> ExecuteAsync(Context _context, CancellationToken token)
        {
            ServerSearcherContext context = _context as ServerSearcherContext;

            return await Task.Run(() => Execute(context, token));

            static Context Execute(ServerSearcherContext context, CancellationToken token)
            {
                context.serverInfos = new();

                foreach (var entry in SettingsManager.ServerCollection.Dump(DateTime.MinValue))
                    context.serverInfos.Add(new(entry.Address, entry.Port));

                return context;
            }
        }
    }

    public class ServerSearcherImpl : MonoBehaviour
    {

    }
}
