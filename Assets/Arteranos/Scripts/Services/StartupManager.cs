/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using Arteranos.Core.Operations;
using Arteranos.Web;
using Ipfs;
using Mirror;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;
using Utils = Arteranos.Core.Utils;

namespace Arteranos.Services
{
    public class StartupManager : SettingsManager
    {
        private bool initialized = false;

        private readonly ConcurrentQueue<Func<IEnumerator>> QueuedCoroutine = new();


        protected override void Awake()
        {
            Instance = this;

            base.Awake();
        }

        protected override void OnDestroy() => Instance = null;

        IEnumerator StartupCoroutine()
        {
            // Startup of dependent services...
            AudioManager.Instance.enabled = true;
            GetComponent<MetaDataService>().enabled = true;
            NetworkStatus.Instance.enabled = true;

            XR.XRControl.Instance.enabled = true;

            yield return new WaitForEndOfFrame();

            if (TargetedPeerID == null && DesiredWorldCid != null)
            {
                ServerSearcher.InitiateServerTransition(DesiredWorldCid);
            }
            else if (TargetedPeerID != null)
            {
                ConnectionManager.ConnectToServer(TargetedPeerID);

                // https://www.youtube.com/watch?v=dQw4w9WgXcQ
                while (NetworkClient.isConnecting) yield return null;

                if (!NetworkClient.isConnected)
                    _ = WorldTransition.EnterWorldAsync(null);
            }
            else
            {
                Task t = WorldTransition.EnterWorldAsync(DesiredWorldCid);
                while(!t.IsCompleted && !t.IsFaulted) yield return null;
            }


            if (FileUtils.Unity_Server)
            {
                // Manually start the server, including with the initialization.
                Task t = NetworkStatus.StartServer();
                while (!t.IsCompleted && !t.IsFaulted) yield return null;
                yield return new WaitForSeconds(5);
                (string address, int _, int mdport) = GetServerConnectionData();
                Debug.Log($"Server is running, launcher link is: http://{address}:{mdport}/");
            }
        }

        protected void Update()
        {
            if(QueuedCoroutine.TryDequeue(out Func<IEnumerator> action))
                StartCoroutine(action());

            if(initialized) return;

            initialized = true;

            // Initialize with the black screen
            XR.ScreenFader.StartFading(1.0f, 0.0f);

            StartCoroutine(StartupCoroutine());
        }

        protected override void PingServerChangeWorld_(string invoker, Cid WorldCid) 
            => _ = ArteranosNetworkManager.Instance.EmitToClientsWCAAsync(invoker, WorldCid);

        protected override void StartCoroutineAsync_(Func<IEnumerator> action) 
            => QueuedCoroutine.Enqueue(action);

        protected override bool IsSelf_(MultiHash ServerPeerID) 
            => IPFSService.Self.Id == ServerPeerID;
    }
}

