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

namespace Arteranos.Web
{

    public class ConnectionManagerImpl : ConnectionManager
    {
        private void Awake() => Instance = this;
        private void OnDestroy() => Instance = null;

        protected override async Task<bool> ConnectToServer_(MultiHash PeerID)
        {
            if (NetworkStatus.GetOnlineLevel() != OnlineLevel.Offline)
            {
                // Anything but idle, cut off all connections before connecting to the desired server.
                await NetworkStatus.StopHost(false);
            }

            ServerInfo si = new(PeerID);
            await si.Update();

            bool? result = null;
            IEnumerator AskForAgreement()
            {
                yield return null;

                AgreementDialogUIFactory.New(si, 
                    () => 
                    { 
                        result = false; 
                    }, 
                    () =>
                    {
                        result = true;
                    });
            }

            // Ask for the privacy notice agreement, or silently agree if it's already known:
            SettingsManager.StartCoroutineAsync(AskForAgreement);

            // Take aim....  hold.... hold.... 
            while(result == null) await Task.Yield();

            // ... Fire!
            if(result == true) return await CommenceConnection(si);

            // Drat. Hangfire.
            return false;
        }

        [Obsolete("TODO PeerID to IPAddress lookup")]
        private async Task<bool> CommenceConnection(ServerInfo si)
        {
            // FIXME Telepathy Transport specific.
            IPAddress addr = IPAddress.IPv6Any;

            Uri connectionUri = new($"tcp4://{addr}:{si.ServerPort}");
            ExpectConnectionResponse_();
            NetworkStatus.StartClient(connectionUri);

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
