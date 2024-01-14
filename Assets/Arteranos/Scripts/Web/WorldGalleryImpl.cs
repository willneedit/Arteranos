/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using Ipfs;
using System;

namespace Arteranos.Web
{
    public class WorldGalleryImpl : WorldGallery
    {
        private void Awake() => Instance = this;
        private void OnDestroy() => Instance = null;


        protected override void FavouriteWorld_(Cid cid)
        {
            Client c = SettingsManager.Client;

            if(!c.WorldList.Contains(cid))
            {
                c.WorldList.Add(cid);
                c.Save();
            }
        }

        protected override void UnfavoriteWorld_(Cid cid)
        {
            Client c = SettingsManager.Client;

            if (c.WorldList.Contains(cid))
            {
                c.WorldList.Remove(cid);
                c.Save();
            }
        }

        protected override bool IsWorldFavourited_(Cid cid)
            => SettingsManager.Client.WorldList.Contains(cid);

        protected override void BumpWorldInfo_(Cid cid)
        {
            WorldInfo wi = WorldInfo.DBLookup(cid);
            if (wi != null)
            {
                wi.Updated = DateTime.Now;
                wi.DBUpdate();
            }
        }

    }
}
