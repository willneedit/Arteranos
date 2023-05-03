/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

using Mirror;
using Utils = Arteranos.Core.Utils;

namespace Arteranos
{
    public class ServerGallery
    {
        private static string GetRootPath(string url) => $"{Application.persistentDataPath}/ServerGallery/{Utils.GetURLHash(url)}";

        public static ServerSettingsJSON RetrieveServerSettings(string url)
        {
            string rootPath = GetRootPath(url);

            string metadataFile = $"{rootPath}/ServerSettings.json";

            if(!File.Exists(metadataFile)) return null;

            string json = File.ReadAllText(metadataFile);
            return JsonConvert.DeserializeObject<ServerSettingsJSON>(json);
        }

        public static void StoreServerSettings(string url, ServerSettingsJSON serverSettings)
        {
            string rootPath = GetRootPath(url);

            string metadataFile = $"{rootPath}/ServerSettings.json";

            Directory.CreateDirectory(rootPath);

            string json = JsonConvert.SerializeObject(serverSettings, Formatting.Indented);
            File.WriteAllText(metadataFile, json);
        }

        public static void DeleteServerSettings(string url)
        {
            string rootPath = GetRootPath(url);

            if(Directory.Exists(rootPath)) Directory.Delete(rootPath, true);
        }

        public static async Task<(string, ServerMetadataJSON)> DownloadServerMetadataAsync(
            string url, 
            int timeout = 20)
        {
            DownloadHandlerBuffer dh = new();
            using UnityWebRequest uwr = new(
                $"{url}/metadata.json", 
                UnityWebRequest.kHttpVerbGET,
                dh,
                null);

            uwr.timeout = timeout;

            UnityWebRequestAsyncOperation uwr_ao = uwr.SendWebRequest();

            while(!uwr_ao.isDone) await Task.Yield();

            ServerMetadataJSON smdj = null;

            if(uwr.result == UnityWebRequest.Result.Success)
                smdj = JsonConvert.DeserializeObject<ServerMetadataJSON>(dh.text);

            return (url, smdj);
        }

        public static async void DownloadServerMetadataAsync(
            string url,
            Action<string, ServerMetadataJSON> callback,
            int timeout = 20)
        {
            string resultUrl;
            ServerMetadataJSON smdj;
            (resultUrl, smdj) = await DownloadServerMetadataAsync(url, timeout);

            callback(resultUrl, smdj);
        }

        public static async Task<bool> ConnectToServer(string serverURL)
        {
            // Only if the client module is idle, not even in the connection pending state.
            if(NetworkClient.active) return false;

            ServerSettingsJSON ssj = RetrieveServerSettings(serverURL);
            
            if(ssj == null)
            {
                Debug.Log($"{serverURL} has no meta data, downloading...");
                ServerMetadataJSON smdj;
                (_, smdj) = await DownloadServerMetadataAsync(serverURL);

                Debug.Log($"Metadata download: {smdj != null}");

                ssj = smdj?.Settings;

                if(ssj == null)
                {
                    Debug.Log("Still no viable metadata, giving up.");
                    return false;
                }

                // Store it for the posterity.
                StoreServerSettings(serverURL, ssj);
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
