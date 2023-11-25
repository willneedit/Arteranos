/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;

namespace Arteranos.Web
{
    public class WorldGalleryImpl : WorldGallery
    {
        private void Awake() => Instance = this;
        private void OnDestroy() => Instance = null;

        public override WorldInfo? GetWorldInfo_(string url)
            => WorldDownloader.GetWorldInfo(url);

        public override void PutWorldInfo_(string url, WorldInfo info)
            => WorldDownloader.PutWorldInfo(url, info);

        public override void FavouriteWorld_(string url)
        {
            Client c = SettingsManager.Client;

            if(!c.WorldList.Contains(url))
            {
                c.WorldList.Add(url);
                c.Save();
            }
        }

        public override void UnfavoriteWorld_(string url)
        {
            Client c = SettingsManager.Client;

            if (c.WorldList.Contains(url))
            {
                c.WorldList.Remove(url);
                c.Save();
            }
        }

        public override bool IsWorldFavourited_(string url)
            => SettingsManager.Client.WorldList.Contains(url);

        public override void BumpWorldInfo_(string url)
        {
            WorldInfo? wi = WorldDownloader.GetWorldInfo(url);
            if (wi != null)
            {
                WorldInfo wiv = wi.Value;
                wiv.updated = System.DateTime.Now;
                WorldDownloader.PutWorldInfo(url, wiv);
            }
        }

    }
}
