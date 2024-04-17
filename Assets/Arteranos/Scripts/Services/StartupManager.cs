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
using Ipfs;
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

            NetworkStatus.Instance.enabled = true;

            XR.XRControl.Instance.enabled = true;

            // First, wait for IPFS to come up.
            yield return new WaitUntil(() => IPFSService.Instance?.Ipfs_ != null);

            yield return UploadDefaultAvatars();

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
        }

        protected void Update()
        {
            if(QueuedCoroutine.TryDequeue(out Func<IEnumerator> action))
            {
                if (action != null)
                    StartCoroutine(action?.Invoke());
                else
                    Debug.LogWarning("Asynced Coroutine: Coroutine's underlying object == null");
            }

            if (initialized) return;

            // Very first frame, every Awake() has been called, everything is a go.
            initialized = true;

            StartCoroutine(StartupCoroutine());
        }

        private IEnumerator UploadDefaultAvatars()
        {
            Cid cid = null;
            IEnumerator UploadAvatar(string resourceMA)
            {
                (AsyncOperationExecutor<Context> ao, Context co) =
                    AssetUploader.PrepareUploadToIPFS(resourceMA, false); // Plsin GLB files

                yield return ao.ExecuteCoroutine(co);

                cid = AssetUploader.GetUploadedCid(co);

            }

            yield return UploadAvatar("resource:///Avatar/6394c1e69ef842b3a5112221.glb"); 
            DefaultMaleAvatar = cid;
            yield return UploadAvatar("resource:///Avatar/63c26702e5b9a435587fba51.glb");
            DefaultFemaleAvatar = cid;
        }


        protected override void StartCoroutineAsync_(Func<IEnumerator> action) 
            => QueuedCoroutine.Enqueue(action);

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

