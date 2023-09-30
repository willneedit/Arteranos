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
using System.ComponentModel.Design.Serialization;

namespace Arteranos.Web
{

    public class ConnectionManagerImpl : MonoBehaviour, IConnectionManager
    {
        public void Awake() => ConnectionManager.Instance = this;
        public void OnDestroy() => ConnectionManager.Instance = null;

        public async Task<bool> ConnectToServer(string serverURL)
        {
            if(NetworkStatus.GetOnlineLevel() != OnlineLevel.Offline)
            {
                // Anything but idle, cut off all connections before connecting to the desired server.
                NetworkStatus.StopHost(false);

                await Task.Delay(3000);
            }

            ServerSettingsJSON ssj = ServerGallery.RetrieveServerSettings(serverURL);

            if(ssj == null)
            {
                Debug.Log($"{serverURL} has no meta data, downloading...");
                ServerMetadataJSON smdj;
                (_, smdj) = await ServerGallery.DownloadServerMetadataAsync(serverURL);

                Debug.Log($"Metadata download: {smdj != null}");

                ssj = smdj?.Settings;

                if(ssj == null)
                {
                    Debug.Log("Still no viable metadata, giving up.");
                    ConnectionResponse(false, null);
                    return false;
                }

                // Store it for the posterity.
                ServerGallery.StoreServerSettings(serverURL, ssj);
            }

            Uri serverURI = Utils.ProcessUriString(serverURL,
                scheme: "http",
                port: ServerSettingsJSON.DefaultMetadataPort,
                path: ServerSettingsJSON.DefaultMetadataPath
                );

            // FIXME Telepathy Transport specific.
            Uri connectionUri = new($"tcp4://{serverURI.Host}:{ssj.ServerPort}");

            NetworkStatus.OnClientConnectionResponse = ConnectionResponse;
            NetworkStatus.StartClient(connectionUri);

            // Here goes nothing...
            return true;
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
