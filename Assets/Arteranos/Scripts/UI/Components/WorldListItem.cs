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

        public string WorldURL { get; internal set; } = null;
        public string WorldName { get; internal set; } = null;
        public byte[] ScreenshotPNG { get; internal set; } = null;
        public DateTime LastAccessed { get; internal set; } = DateTime.MinValue;
        public int ServersCount { get; internal set; } = 0;
        public int UsersCount { get; internal set; } = 0;
        public int FriendsMax { get; internal set; } = 0;
        public bool AllowedForThis {  get; internal set; } = true;

        private string patternCaption = null;

        public static WorldListItem New(Transform parent, string url)
        {
            GameObject go = Instantiate(Resources.Load<GameObject>("UI/Components/WorldListItem"));
            go.transform.SetParent(parent, false);
            WorldListItem worldListItem = go.GetComponent<WorldListItem>();
            worldListItem.WorldURL = url;
            return worldListItem;
        }

        protected override void Awake()
        {
            base.Awake();

            patternCaption = lbl_Caption.text;

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

            PopulateWorldData(WorldURL);
        }

        private void PopulateWorldData(string worldURL)
        {
            IEnumerator VisCoroutine()
            {
                yield return null;

                if (!string.IsNullOrEmpty(WorldName))
                    VisualizeWorldData();
                else
                    lbl_Caption.text = $"({worldURL})";
            }

            StartCoroutine(VisCoroutine());
        }

        private void VisualizeWorldData()
        {
            // If we're in Host mode, you're the admin of your own server, so we're able to
            // change the world. And you still have the great responsibility...
            btn_Visit.gameObject.SetActive(NetworkStatus.GetOnlineLevel() != OnlineLevel.Host);
            btn_ChangeWorld.gameObject.SetActive(
                Utils.IsAbleTo(Social.UserCapabilities.CanInitiateWorldTransition, null)
                && AllowedForThis);

            btn_Add.gameObject.SetActive(!WorldGallery.IsWorldFavourited(WorldURL));
            btn_Delete.gameObject.SetActive(WorldGallery.IsWorldFavourited(WorldURL));


            if(ScreenshotPNG != null)
                Utils.ShowImage(ScreenshotPNG, img_Screenshot);


            string lvstr = (LastAccessed == DateTime.MinValue)
                ? "Never"
                : LastAccessed.ToShortDateString();

            lbl_Caption.text = string.Format(patternCaption,
                WorldName,
                lvstr,
                ServersCount,
                UsersCount,
                FriendsMax);
        }

        [Obsolete("URL -> Cid conversion")]
        private void OnVisitClicked(bool inPlace)
        {
            if(!string.IsNullOrEmpty(WorldURL))
            {
                if(inPlace)
                    WorldTransition.EnterWorldAsync(WorldURL);
                else
                    ServerSearcher.InitiateServerTransition(WorldURL);

                WorldGallery.BumpWorldInfo(WorldURL);
            }
        }

        private void OnAddClicked()
        {
            WorldGallery.FavouriteWorld(WorldURL);
            PopulateWorldData(WorldURL);
        }

        private void OnDeleteClicked()
        {
            WorldGallery.UnfavoriteWorld(WorldURL);
            PopulateWorldData(WorldURL);
        }
    }
}
