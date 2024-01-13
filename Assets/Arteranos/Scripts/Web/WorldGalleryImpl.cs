/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Arteranos.Web
{
    public class WorldGalleryImpl : WorldGallery
    {
        private void Awake() => Instance = this;
        private void OnDestroy() => Instance = null;

        protected override WorldInfo GetWorldInfo_(string url)
            => WorldDownloader.GetWorldInfo(url);

        protected override async Task<WorldInfo> LoadWorldInfoAsync_(string url, CancellationToken token)
        {
            WorldInfo wi = WorldDownloader.GetWorldInfo(url);
            if (wi != null) return wi;

            UriBuilder uriBuilder = new(url);
            uriBuilder.Path = Path.ChangeExtension(uriBuilder.Path, "info");

            using UnityWebRequest uwr = new(uriBuilder.ToString(), UnityWebRequest.kHttpVerbGET);
            uwr.downloadHandler = new DownloadHandlerBuffer();

            UnityWebRequestAsyncOperation uwr_ao = uwr.SendWebRequest();

            while (!uwr_ao.isDone && !token.IsCancellationRequested) await Task.Yield();

            if (uwr.result == UnityWebRequest.Result.ProtocolError || uwr.result == UnityWebRequest.Result.ConnectionError)
                return null;

            try
            {
                wi = DERSerializer.Serializer.Deserialize<WorldInfo>(uwr.downloadHandler.data);
                PutWorldInfo(wi);
            }
            catch 
            {
                // Invalid ASN.1 DER coding for WorldInfo.
                wi = null;
            }

            return wi;
        }

        protected override void PutWorldInfo_(WorldInfo info)
            => WorldDownloader.PutWorldInfo(info);

        protected override void FavouriteWorld_(string url)
        {
            Client c = SettingsManager.Client;

            if(!c.WorldList.Contains(url))
            {
                c.WorldList.Add(url);
                c.Save();
            }
        }

        protected override void UnfavoriteWorld_(string url)
        {
            Client c = SettingsManager.Client;

            if (c.WorldList.Contains(url))
            {
                c.WorldList.Remove(url);
                c.Save();
            }
        }

        protected override bool IsWorldFavourited_(string url)
            => SettingsManager.Client.WorldList.Contains(url);

        protected override void BumpWorldInfo_(string url)
        {
            WorldInfo wi = WorldDownloader.GetWorldInfo(url);
            if (wi != null)
            {
                wi.Updated = DateTime.Now;
                WorldDownloader.PutWorldInfo(wi);
            }
        }

    }
}
