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
using Arteranos.Services;

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
                ProgressChanged?.Invoke(actualServer / serverCount);
                await info.Update(1);
                actualServer++;
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
                => ScoreServer(y) - ScoreServer(x);

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

    public class ServerSearcherImpl : MonoBehaviour, IServerSearcher
    {
        private void Awake() => ServerSearcher.Instance = this;
        private void OnDestroy() => ServerSearcher.Instance = null;

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

        public void InitiateServerTransition(string worldURL)
        {
            InitiateServerTransition(worldURL, OnGotSearchResult, null);
        }

        public void InitiateServerTransition(string worldURL, Action<string, string> OnSuccessCallback, Action OnFailureCallback)
        {
            IProgressUI pui = ProgressUIFactory.New();

            pui.SetupAsyncOperations(() => PrepareSearchServers(worldURL));

            pui.Completed += context => OnSuccessCallback(worldURL, (context as ServerSearcherContext).resultServerURL);
            pui.Faulted += (ex, context) => OnFailureCallback();
        }

        private static void OnGotSearchResult(string worldURL, string serverURL)
        {
            // No matching server, initiate Start Host with loading the world on entering
            if (!string.IsNullOrEmpty(worldURL) && serverURL == null)
            {
                switch(NetworkStatus.GetOnlineLevel())
                {
                    case OnlineLevel.Host:
                    case OnlineLevel.Server:
                        // Host mode. It's the own server.
                        WorldTransition.InitiateTransition(worldURL);
                        break;
                    case OnlineLevel.Offline:
                        // Offline mode, start up the host mode and feed it with the startup world
                    case OnlineLevel.Client:
                        // Client mode. It's time to part ways...
                        SettingsManager.Server.WorldURL = worldURL;
                        NetworkStatus.StartHost(true);
                        break;

                    default: throw new NotImplementedException();
                }
            }

            // No matching server, leave it be
            if (serverURL == null) return;

            // Matching server (with matching world, if needed), initiate remote connection
            ConnectionManager.ConnectToServer(serverURL);
        }
    }
}