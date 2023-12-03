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

namespace Arteranos.Web
{

    public class ConnectionManagerImpl : ConnectionManager
    {
        private void Awake() => Instance = this;
        private void OnDestroy() => Instance = null;

        protected override async Task<bool> ConnectToServer_(string serverURL)
        {
            if (NetworkStatus.GetOnlineLevel() != OnlineLevel.Offline)
            {
                // Anything but idle, cut off all connections before connecting to the desired server.
                await NetworkStatus.StopHost(false);
            }

            ServerInfo si = new(serverURL);
            await si.Update();

            // FIXME Telepathy Transport specific.
            Uri connectionUri = new($"tcp4://{si.Address}:{si.ServerPort}");
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
            
            UI.IDialogUI dialog = UI.DialogUIFactory.New();
            dialog.Buttons = new[] { "Okay" };
            dialog.Text = message;
        }

        protected override void DeliverDisconnectReason_(string reason) => this.reason = reason;
    }
}
