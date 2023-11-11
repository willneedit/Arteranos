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
using Arteranos.UI;

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
        public string Caption => "Fetching servers";
        public Action<float> ProgressChanged { get; set; }

        public async Task<Context> ExecuteAsync(Context _context, CancellationToken token)
        {
            ServerSearcherContext context = _context as ServerSearcherContext;

            return await Task.Run(() => Execute(context, token));

            static Context Execute(ServerSearcherContext context, CancellationToken token)
            {
                context.serverInfos = new();
                context.resultServerURL = null;

                foreach (var entry in SettingsManager.ServerCollection.Dump(DateTime.MinValue))
                    context.serverInfos.Add(new(entry.Address, entry.Port));

                return context;
            }
        }
    }

    internal class UpdateServersOp : IAsyncOperation<Context>
    {
        public int Timeout { get; set; }
        public float Weight { get; set; } = 8.0f;
        public string Caption => $"Updating servers ({actualServer} of {serverCount})";
        public Action<float> ProgressChanged { get; set; }

        private int actualServer = 0;
        private int serverCount = 0;

        public async Task<Context> ExecuteAsync(Context _context, CancellationToken token)
        {
            ServerSearcherContext context = _context as ServerSearcherContext;

            async Task UpdateOne(ServerInfo info)
            {
                await info.Update(10);
                actualServer++;
                ProgressChanged?.Invoke(actualServer / serverCount);
            }

            serverCount = context.serverInfos.Count;

            TaskPool<ServerInfo> pool = new(10);

            foreach (ServerInfo info in context.serverInfos)
                pool.Schedule(info, UpdateOne);

            await pool.Run(token);

            return context;
        }
    }

    internal class SortServersOp : IAsyncOperation<Context>
    {
        public int Timeout { get; set; }
        public float Weight { get; set; } = 1.0f;
        public string Caption => "Sorting";
        public Action<float> ProgressChanged { get; set; }

        public async Task<Context> ExecuteAsync(Context _context, CancellationToken token)
        {
            ServerSearcherContext context = _context as ServerSearcherContext;

            return await Task.Run(() => Execute(context, token));

            int ScoreServer(ServerInfo x)
            {
                int xScore = x.MatchScore;
                if (context.desiredWorldURL != null 
                    && x.CurrentWorld != context.desiredWorldURL) xScore = -10000;
                return xScore;
            }

            int CompareServers(ServerInfo x, ServerInfo y)
                => ScoreServer(x) - ScoreServer(y);

            Context Execute(ServerSearcherContext context, CancellationToken token)
            {
                context.serverInfos.Sort(CompareServers);

                ServerInfo leader = context.serverInfos.Count > 0 ? context.serverInfos[0] : null;

                // Even the leader is disqualified, there's no winner.
                if (leader != null && ScoreServer(leader) < 0) leader = null;

                // ... And the winner is... *drumroll*
                if (leader != null) context.resultServerURL = leader.URL;

                return context;
            }

        }

    }

    public class ServerSearcherImpl : MonoBehaviour
    {
        public static (AsyncOperationExecutor<Context>, Context) PrepareSearchServers(string desiredWorld)
        {
            ServerSearcherContext context = new()
            {
                desiredWorldURL = desiredWorld
            };

            AsyncOperationExecutor<Context> executor = new(new IAsyncOperation<Context>[]
            {
                new FetchServersOp(),
                new UpdateServersOp(),
                new SortServersOp()
            });

            return (executor, context);
        }

        public static void InitiateServerTransition(string worldURL, Action<string> OnSuccessCallback, Action OnFailureCallback)
        {
            IProgressUI pui = ProgressUIFactory.New();

            pui.SetupAsyncOperations(() => PrepareSearchServers(worldURL));

            pui.Completed += context => OnSuccessCallback((context as ServerSearcherContext).resultServerURL);
            pui.Faulted += (ex, context) => OnFailureCallback();
        }
    }
}
