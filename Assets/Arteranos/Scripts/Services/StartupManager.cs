/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Avatar;
using Arteranos.Core;
using Arteranos.Core.Operations;
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

            NetworkStatus.Instance.enabled = true;

            G.XRControl.enabled = true;

            yield return TransitionProgressStatic.TransitionFrom();

            yield return new WaitUntil(() => TransitionProgressStatic.Instance != null);

            TransitionProgressStatic.Instance.OnProgressChanged(0.00f, "Starting up");

            IPFSService.Instance.enabled = true;

            // First, wait for IPFS to come up.
            yield return new WaitUntil(() => IPFSService.Instance?.Ipfs_ != null);

            if (DesiredWorldCid != null)
                ServerSearcher.InitiateServerTransition(DesiredWorldCid);
            else if (TargetedPeerID != null)
                yield return ConnectionManager.ConnectToServer(TargetedPeerID, null);
            else
                yield return TransitionProgressStatic.TransitionTo(null, null);


            // TODO Dedicated server: Startup world commandline argument processing
            if (FileUtils.Unity_Server)
            {
                // Manually start the server, including with the initialization.
                Task t = NetworkStatus.StartServer();
                while (!t.IsCompleted) yield return null;
                yield return new WaitForSeconds(5);
                Debug.Log($"Server is running, launch argument is: arteranos://{IPFSService.Self.Id}/");
            }

            enabled = false;
        }

        protected void Update()
        {
            if (initialized) return;

            // Very first frame, every Awake() has been called, everything is a go.
            initialized = true;

            StartCoroutine(StartupCoroutine());
        }

        protected override void EmitToClientCTSPacket_(CTSPacket packet, IAvatarBrain to = null) 
            => ArteranosNetworkManager.Instance.EmitToClientCTSPacket(packet, to);

        protected override void EmitToServerCTSPacket_(CTSPacket packet) 
            => ArteranosNetworkManager.Instance.EmitToServerCTSPacket(packet);


        protected override event Action<UserID, ServerUserState> OnClientReceivedServerUserStateAnswer_
        {
            add => ArteranosNetworkManager.Instance.OnClientReceivedServerUserStateAnswer += value;
            remove { if (ArteranosNetworkManager.Instance != null) ArteranosNetworkManager.Instance.OnClientReceivedServerUserStateAnswer -= value; }
        }

        protected override event Action<ServerJSON> OnClientReceivedServerConfigAnswer_
        {
            add => ArteranosNetworkManager.Instance.OnClientReceivedServerConfigAnswer += value;
            remove { if (ArteranosNetworkManager.Instance != null) ArteranosNetworkManager.Instance.OnClientReceivedServerConfigAnswer -= value;  }
        }
    }
}

