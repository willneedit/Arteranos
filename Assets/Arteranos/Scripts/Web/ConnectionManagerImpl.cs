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
                    ConnectionResponse(false);
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

        private void ConnectionResponse(bool success)
        {
            NetworkStatus.OnClientConnectionResponse = null;

            if(success) return;

            UI.IDialogUI dialog = UI.DialogUIFactory.New();
            dialog.Buttons = new[] { "Okay" };
            dialog.Text =
                "Failed to connect to this server.\n" +
                "Maybe it's offline.";
        }
    }
}
