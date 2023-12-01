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

    internal class ServerSearcherContext : Context
    {
        public List<ServerInfo> serverInfos = null;
        public string desiredWorldURL = null;
        public ServerPermissions desiredWorldPermissions = null;
        public string resultServerURL = null;
    }

    internal class PreloadServerRequirementsOp : IAsyncOperation<Context>
    {
        public int Timeout { get; set; }
        public float Weight { get; set; } = 0.1f;
        public string Caption => "Preloading";
        public Action<float> ProgressChanged { get; set; }

        public async Task<Context> ExecuteAsync(Context _context, CancellationToken token)
        {
            ServerSearcherContext context = _context as ServerSearcherContext;

            WorldInfo? wi = await WorldGallery.LoadWorldInfoAsync(context.desiredWorldURL, token);

            context.desiredWorldPermissions = wi?.metaData.ContentRating;

            return context;
        }
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
                    context.serverInfos.Add(new(entry));

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
                await info.Update();
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
                if (!x.IsOnline)
                {
                    //Debug.Log($"{x.URL} is offline");
                    xScore = -20000;
                }
                else if (context.desiredWorldPermissions != null && context.desiredWorldPermissions.IsInViolation(x.Permissions))
                {
                    //Debug.Log($"{x.URL} is too restrictive for the desired world");
                    xScore = -10000;
                }
                else if (context.desiredWorldURL != null && x.CurrentWorld != context.desiredWorldURL)
                {
                    //Debug.Log($"{x.URL} loaded a different world");
                    xScore = -10000;
                }

                return xScore;
            }

            int CompareServers(ServerInfo x, ServerInfo y)
                => ScoreServer(y) - ScoreServer(x);

            Context Execute(ServerSearcherContext context, CancellationToken token)
            {
                context.serverInfos.Sort(CompareServers);

                ServerInfo leader = context.serverInfos.Count > 0 ? context.serverInfos[0] : null;

                int score = ScoreServer(leader);

                if (leader == null)
                    Debug.Log("Server search result: None at all.");
                else
                    Debug.Log($"Server search winner: {leader.Name}, Score: {score}");
                // Even the leader is disqualified, there's no winner.
                if (leader != null && score < 0) leader = null;

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
                new PreloadServerRequirementsOp(),
                new FetchServersOp(),
                new UpdateServersOp(),
                new SortServersOp()
            });

            return (executor, context);
        }

        public void InitiateServerTransition(string worldURL)
        {
            static void GotResult(string worldURL, string serverURL) 
                => _ = OnGotSearchResult(worldURL, serverURL);

            InitiateServerTransition(worldURL, GotResult, null);
        }

        public void InitiateServerTransition(string worldURL, Action<string, string> OnSuccessCallback, Action OnFailureCallback)
        {
            IProgressUI pui = ProgressUIFactory.New();

            pui.SetupAsyncOperations(() => PrepareSearchServers(worldURL));

            pui.Completed += context => OnSuccessCallback(worldURL, (context as ServerSearcherContext).resultServerURL);
            pui.Faulted += (ex, context) => OnFailureCallback();
        }

        private static async Task OnGotSearchResult(string worldURL, string serverURL)
        {
            // No matching server, initiate Start Host with loading the world on entering
            if (!string.IsNullOrEmpty(worldURL) && serverURL == null)
            {
                // It's time to part ways...
                if(NetworkStatus.GetOnlineLevel() == OnlineLevel.Client)
                    await NetworkStatus.StopHost(true);

                await WorldTransition.EnterWorldAsync(worldURL);

                // If we haven't a server (or, just left one), start up.
                if(NetworkStatus.GetOnlineLevel() == OnlineLevel.Offline)
                    await NetworkStatus.StartHost();
            }

            // No matching server, leave it be
            if (serverURL == null) return;

            if(SettingsManager.IsSelf(new Uri(serverURL)))
            {
                Debug.Log("...It's us! :O");
                return;
            }

            XR.ScreenFader.StartFading(1.0f);

            await Task.Delay(1000);

            // Matching server (with matching world, if needed), initiate remote connection
            await ConnectionManager.ConnectToServer(serverURL);
        }
    }
}
