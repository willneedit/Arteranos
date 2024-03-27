/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using System;
using System.Threading.Tasks;
using UnityEngine;

using Arteranos.Services;
using System.Collections;
using Arteranos.UI;
using Ipfs;
using System.Net;
using System.Threading;

namespace Arteranos.Web
{

    public class ConnectionManagerImpl : ConnectionManager
    {
        private void Awake() => Instance = this;
        private void OnDestroy() => Instance = null;

        protected override IEnumerator ConnectToServer_(MultiHash PeerID, Action<bool> callback)
        {

            bool? result = null;

            if (NetworkStatus.GetOnlineLevel() != OnlineLevel.Offline)
            {
                // Anything but idle, cut off all connections before connecting to the desired server.
                Task t0 = NetworkStatus.StopHost(false);

                yield return new WaitUntil(() => t0.IsCompleted);
            }

            ServerInfo si = new(PeerID);

            AgreementDialogUIFactory.New(si, () => result = false, () => result = true);

            yield return new WaitUntil(() => result != null);

            // User denied privacy agreement, backing out.
            if (result == false)
            {
                callback?.Invoke(false);
                yield break;
            }

            yield return CommenceConnection(si, _result => result = _result);

            callback?.Invoke(result.Value);
        }

        private IEnumerator CommenceConnection(ServerInfo si, Action<bool> callback)
        {
            IPAddress addr = IPAddress.Any;

            // In any case, go into the transitional phase.
            yield return TransitionProgressStatic.TransitionFrom();

            TransitionProgressStatic.Instance.OnProgressChanged(0.10f, "Searching server");

            Task<IPAddress> taskIPAddr = Task.Run(async () =>
            {
                using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));
                return await IPFSService.GetPeerIPAddress(si.PeerID.ToString(), cts.Token);
            });

            yield return new WaitUntil(() => taskIPAddr.IsCompleted);

            if(!taskIPAddr.IsCompletedSuccessfully)
            {
                Debug.Log($"{si.PeerID} is unreachable.");
                yield return TransitionProgressStatic.TransitionTo(null, null);
                callback?.Invoke(false);
                yield break;
            }

            addr = taskIPAddr.Result;

            TransitionProgressStatic.Instance.OnProgressChanged(0.50f, "Connecting...");

            // FIXME Telepathy Transport specific.
            Uri connectionUri = new($"tcp4://{addr}:{si.ServerPort}");
            ExpectConnectionResponse_();
            NetworkStatus.StartClient(connectionUri);

            // https://www.youtube.com/watch?v=dQw4w9WgXcQ
            while (NetworkStatus.IsClientConnecting) yield return new WaitForEndOfFrame();

            // Save it for now even before the connection negotiation and authentication
            NetworkStatus.RemotePeerId = si.PeerID;

            // Client failed to connect. Maybe an invalid IP, or a misconfigured firewall.
            // Fall back to the offline world.
            if (!NetworkStatus.IsClientConnected)
            {
                callback?.Invoke(false);
                yield return TransitionProgressStatic.TransitionTo(null, null);
                yield break;
            }

            callback?.Invoke(true);
            // Just-connected server will tell which world we're going to.
            // If the server is borked, the disconnecting server will cause the client to
            // fall back to the offline world.
        }

        protected override void ExpectConnectionResponse_()
        {
            NetworkStatus.OnClientConnectionResponse = ConnectionResponse;
        }

        bool wasOnline = false;
        string reason = null;

        private void ConnectionResponse(bool success, string message)
        {
            Debug.Log($"Onlinelevel={NetworkStatus.GetOnlineLevel()}, Success={success}, wasOnline={wasOnline}, reason={reason}");

            if(!success)
            {
                bool remoteDisconnected = (NetworkStatus.GetOnlineLevel() == OnlineLevel.Client 
                    || NetworkStatus.GetOnlineLevel() == OnlineLevel.Host);

                if (reason != null)
                {
                    message = reason;
                    reason = null;
                }
                else if (message == null)
                {
                    // Only if you're disconnecting of your own volition
                    if (remoteDisconnected && wasOnline) message = "Disconnected from the server.";
                    // Only if we were never online in the first place
                    else if (!wasOnline) message = "Failed to connect to this server.";
                }
            }
            wasOnline = success;
            // NetworkStatus.OnClientConnectionResponse = null;

            if(message == null) return;
            
            IDialogUI dialog = DialogUIFactory.New();
            dialog.Buttons = new[] { "Okay" };
            dialog.Text = message;
        }

        protected override void DeliverDisconnectReason_(string reason) => this.reason = reason;
    }
}
