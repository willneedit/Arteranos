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

using Mirror;

namespace Arteranos.Web
{
    public class ConnectionManager
    {
        public static async Task<bool> ConnectToServer(string serverURL)
        {
            // Only if the client module is idle, not even in the connection pending state.
            if(NetworkClient.active) return false;

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
                    return false;
                }

                // Store it for the posterity.
                ServerGallery.StoreServerSettings(serverURL, ssj);
            }

            Uri serverURI = new(serverURL);

            // FIXME Telepathy Transport specific.
            Uri connectionUri = new($"tcp4://{serverURI.Host}:{ssj.ServerPort}");

            NetworkManager manager = GameObject.FindObjectOfType<NetworkManager>();

            Debug.Log($"Attempting to connect to {connectionUri.ToString()}...");

            manager.StartClient(connectionUri);

            // Here goes nothing...
            return true;
        }

        /// <summary>
        /// Able to connect outgoing connections?
        /// </summary>
        /// <param name="serverURL">The server you ant to connect to</param>
        /// <returns>true if the client is inactive</returns>
        public static bool CanDoConnect() => (!NetworkClient.active && !NetworkServer.active);

        /// <summary>
        /// Ready to listen to incoming connections?
        /// </summary>
        public static bool CanGetConnected() => NetworkServer.active;
    }
}
