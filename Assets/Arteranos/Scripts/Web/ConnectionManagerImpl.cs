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

    public class ConnectionManagerImpl : MonoBehaviour, IConnectionManager
    {
        public void Awake() => ConnectionManager.Instance = this;
        public void OnDestroy() => ConnectionManager.Instance = null;

        public async Task<bool> ConnectToServer(string serverURL)
        {
            if (NetworkStatus.GetOnlineLevel() != OnlineLevel.Offline)
            {
                // Anything but idle, cut off all connections before connecting to the desired server.
                await NetworkStatus.StopHost(false);
            }

            ServerOnlineData? sod = ServerGallery.RetrieveServerSettings(serverURL);

            if (sod == null)
            {
                Debug.Log($"{serverURL} has no meta data, downloading...");
                (_, sod) = await ServerPublicData.GetServerDataAsync(serverURL);

                Debug.Log($"Metadata download: {sod != null}");

                if (sod == null)
                {
                    Debug.Log("Still no viable metadata, giving up.");
                    ConnectionResponse(false, null);
                    return false;
                }
                else ServerGallery.StoreServerSettings(serverURL, sod.Value);
            }

            Uri serverURI = Utils.ProcessUriString(serverURL,
                scheme: "http",
                port: ServerJSON.DefaultMetadataPort,
                path: ServerJSON.DefaultMetadataPath
                );

            // FIXME Telepathy Transport specific.
            Uri connectionUri = new($"tcp4://{serverURI.Host}:{sod?.ServerPort}");
            ExpectConnectionResponse();
            NetworkStatus.StartClient(connectionUri);

            // Here goes nothing...
            return true;
        }

        public void ExpectConnectionResponse()
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

        public void DeliverDisconnectReason(string reason) => this.reason = reason;
    }
}
