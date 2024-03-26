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

        protected override Task<bool> ConnectToServer_(MultiHash PeerID)
        {

            bool? result = null;
            bool done = false;
            IEnumerator AskForAgreement()
            {
                if (NetworkStatus.GetOnlineLevel() != OnlineLevel.Offline)
                {
                    // Anything but idle, cut off all connections before connecting to the desired server.
                    Task t0 = NetworkStatus.StopHost(false);

                    yield return new WaitUntil(() => t0.IsCompleted);
                }

                ServerInfo si = new(PeerID);

                AgreementDialogUIFactory.New(si, () => result = false, () => result = true);

                yield return new WaitUntil(() => result !=  null);

                // User denied privacy agreement, backing out.
                if (result == false)
                {
                    done = true;
                    yield break;
                }

                Task<bool> t = CommenceConnection(si);

                yield return new WaitUntil(() => t.IsCompleted);

                result = t.Result;
                done = true;
            }


            Task<bool> t1 = Task.Run(async () =>
            {
                // Ask for the privacy notice agreement, or silently agree if it's already known:
                SettingsManager.StartCoroutineAsync(AskForAgreement);

                while (!done) await Task.Delay(8);
                return result.Value;
            });

            return t1;
        }

        private async Task<bool> CommenceConnection(ServerInfo si)
        {
            IPAddress addr = IPAddress.Any;

            try
            {
                using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));
                addr = await IPFSService.GetPeerIPAddress(si.PeerID.ToString(), cts.Token);
            }
            catch
            {
                Debug.Log($"{si.PeerID} is unreachable.");
                return false;
            }

            // FIXME Telepathy Transport specific.
            Uri connectionUri = new($"tcp4://{addr}:{si.ServerPort}");
            ExpectConnectionResponse_();
            NetworkStatus.StartClient(connectionUri);

            // Save it for now even before the connection negotiation and authentication
            NetworkStatus.RemotePeerId = si.PeerID;

            // Here goes nothing...
            return true;
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
