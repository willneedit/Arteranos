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
using Utils = Arteranos.Core.Utils;

namespace Arteranos.Web
{
    public class ServerGalleryImpl : MonoBehaviour, IServerGallery
    {
        private void Awake() => ServerGallery.Instance = this;
        private void OnDestroy() => ServerGallery.Instance = null;

        private static string GetRootPath(string url) => $"{Application.persistentDataPath}/ServerGallery/{Utils.GetURLHash(url)}";

        public ServerJSON RetrieveServerSettings(string url)
        {
            string rootPath = GetRootPath(url);

            string metadataFile = $"{rootPath}/ServerSettings.json";

            if(!File.Exists(metadataFile)) return null;

            string json = File.ReadAllText(metadataFile);
            return JsonConvert.DeserializeObject<ServerJSON>(json);
        }

        public void StoreServerSettings(string url, ServerJSON serverSettings)
        {
            string rootPath = GetRootPath(url);

            string metadataFile = $"{rootPath}/ServerSettings.json";

            Directory.CreateDirectory(rootPath);

            string json = JsonConvert.SerializeObject(serverSettings, Formatting.Indented);
            File.WriteAllText(metadataFile, json);
        }

        public void DeleteServerSettings(string url)
        {
            string rootPath = GetRootPath(url);

            if(Directory.Exists(rootPath)) Directory.Delete(rootPath, true);
        }

        public async Task<(string, ServerMetadataJSON)> DownloadServerMetadataAsync(
            string url,
            int timeout = 20)
        {
            Uri uri = Utils.ProcessUriString(url,
                scheme: "http",
                port: ServerJSON.DefaultMetadataPort,
                path: ServerJSON.DefaultMetadataPath
                );

            DownloadHandlerBuffer dh = new();
            using UnityWebRequest uwr = new(
                uri,
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

        public async void DownloadServerMetadataAsync(
            string url,
            Action<string, ServerMetadataJSON> callback,
            int timeout = 20)
        {
            string resultUrl;
            ServerMetadataJSON smdj;
            (resultUrl, smdj) = await DownloadServerMetadataAsync(url, timeout);

            callback(resultUrl, smdj);
        }

    }
}
