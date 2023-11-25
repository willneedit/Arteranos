/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Arteranos.Web;
using System.IO;
using UnityEngine.Networking;
using Arteranos.Core;
using Arteranos.Services;

namespace Arteranos.UI
{
    public class WorldListItem : ListItemBase
    {
        private HoverButton btn_Add = null;
        private HoverButton btn_Visit = null;
        private HoverButton btn_Delete = null;
        private HoverButton btn_ChangeWorld = null;

        public Image img_Screenshot = null;
        public TMP_Text lbl_Caption = null;

        private string worldURL = null;

        public static WorldListItem New(Transform parent, string url)
        {
            GameObject go = Instantiate(Resources.Load<GameObject>("UI/Components/WorldListItem"));
            go.transform.SetParent(parent, false);
            WorldListItem worldListItem = go.GetComponent<WorldListItem>();
            worldListItem.worldURL = url;
            return worldListItem;
        }

        protected override void Awake()
        {
            base.Awake();

            btn_Add = btns_ItemButton[0];
            btn_Visit= btns_ItemButton[1];
            btn_Delete= btns_ItemButton[2];
            btn_ChangeWorld = btns_ItemButton[3];

            btn_Add.onClick.AddListener(OnAddClicked);
            btn_Visit.onClick.AddListener(() => OnVisitClicked(false));
            btn_Delete.onClick.AddListener(OnDeleteClicked);
            btn_ChangeWorld.onClick.AddListener(() => OnVisitClicked(true));
        }

        protected override void Start()
        {
            base.Start();

            if(!string.IsNullOrEmpty(worldURL)) PopulateWorldData(worldURL);
        }

        private void PopulateWorldData(string worldURL)
        {
            btn_Add.gameObject.SetActive(true);
            btn_Delete.gameObject.SetActive(true);

            // If we're in Host mode, you're the admin of your own server, so we're able to
            // change the world. And you still have the great responsibility...
            btn_Visit.gameObject.SetActive(NetworkStatus.GetOnlineLevel() != OnlineLevel.Host);
            btn_ChangeWorld.gameObject.SetActive(Utils.IsAbleTo(Social.UserCapabilities.CanInitiateWorldTransition, null));

            WorldInfo? wi = WorldGallery.GetWorldInfo(worldURL);

            btn_Add.gameObject.SetActive(!WorldGallery.IsWorldFavourited(worldURL));
            btn_Delete.gameObject.SetActive(WorldGallery.IsWorldFavourited(worldURL));

            if (wi != null)
                VisualizeWorldData(wi.Value);
            else
                lbl_Caption.text = $"({worldURL})";

        }

        private void VisualizeWorldData(WorldInfo wi)
        {
            WorldMetaData wmd = wi.metaData;
            if(wmd?.ContentRating != null && wmd.ContentRating.IsInViolation(SettingsManager.ActiveServerData.Permissions))
            {
                btn_ChangeWorld.gameObject.SetActive(false);
            }

            if(wi.screenshotPNG != null)
                Utils.ShowImage(wi.screenshotPNG, img_Screenshot);


            string lvstr = (wi.updated == DateTime.MinValue)
                ? "Never"
                : wi.updated.ToShortDateString();

            lbl_Caption.text = $"{wmd.WorldName}\nLast visited: {lvstr}";
        }

        private void OnVisitClicked(bool inPlace)
        {
            if(!string.IsNullOrEmpty(worldURL))
            {
                if(inPlace)
                    WorldTransition.EnterWorldAsync(worldURL);
                else
                    ServerSearcher.InitiateServerTransition(worldURL);

                WorldGallery.BumpWorldInfo(worldURL);
            }
        }

        private void OnAddClicked()
        {
            WorldGallery.FavouriteWorld(worldURL);
            PopulateWorldData(worldURL);
        }

        private void OnDeleteClicked()
        {
            WorldGallery.UnfavoriteWorld(worldURL);
            // And, zip, gone.);
        }
    }
}
