/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using Arteranos.Web;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;

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

            if (!string.IsNullOrEmpty(TargetedServerPort))
            {
                Uri uri = Utils.ProcessUriString(TargetedServerPort,
                    scheme: "http",
                    port: ServerJSON.DefaultMetadataPort
                );

                ConnectionManager.ConnectToServer(uri.ToString());
            }
            else
            {
                Task t = WorldTransition.EnterWorldAsync(DesiredWorld);
                while(!t.IsCompleted && !t.IsFaulted) yield return null;
            }


            if(FileUtils.Unity_Server)
                // Manually start the server, including with the initialization.
                NetworkStatus.StartServer();
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

        protected override void PingServerChangeWorld_(string invoker, string worldURL) 
            => _ = ArteranosNetworkManager.Instance.EmitToClientsWCAAsync(invoker, worldURL, false);

        protected override void StartCoroutineAsync_(Func<IEnumerator> action) 
            => QueuedCoroutine.Enqueue(action);
    }
}

