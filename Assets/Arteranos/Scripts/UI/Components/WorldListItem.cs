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
using System.Threading.Tasks;
using System.Threading;

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

            lbl_Caption.text = "Loading...";

            _ = PopulateWorldData(worldURL);
        }

        private async Task PopulateWorldData(string worldURL)
        {
            using CancellationTokenSource cts = new();
            WorldInfo? wi = await WorldGallery.LoadWorldInfoAsync(worldURL, cts.Token);

            IEnumerator VisCoroutine()
            {
                yield return null;

                if (wi != null)
                    VisualizeWorldData(wi.Value);
                else
                    lbl_Caption.text = $"({worldURL})";
            }

            SettingsManager.StartCoroutineAsync(VisCoroutine);
        }

        private void VisualizeWorldData(WorldInfo wi)
        {
            // If we're in Host mode, you're the admin of your own server, so we're able to
            // change the world. And you still have the great responsibility...
            btn_Visit.gameObject.SetActive(NetworkStatus.GetOnlineLevel() != OnlineLevel.Host);
            btn_ChangeWorld.gameObject.SetActive(Utils.IsAbleTo(Social.UserCapabilities.CanInitiateWorldTransition, null));

            btn_Add.gameObject.SetActive(!WorldGallery.IsWorldFavourited(worldURL));
            btn_Delete.gameObject.SetActive(WorldGallery.IsWorldFavourited(worldURL));

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
            _ = PopulateWorldData(worldURL);
        }

        private void OnDeleteClicked()
        {
            WorldGallery.UnfavoriteWorld(worldURL);
            _ = PopulateWorldData(worldURL);
        }
    }
}
