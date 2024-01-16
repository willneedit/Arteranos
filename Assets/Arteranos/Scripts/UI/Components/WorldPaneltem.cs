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
using Arteranos.Core;
using Arteranos.Services;
using Ipfs;

namespace Arteranos.UI
{
    public class WorldPaneltem : ListItemBase
    {
        private HoverButton btn_Add = null;
        private HoverButton btn_Visit = null;
        private HoverButton btn_Delete = null;
        private HoverButton btn_ChangeWorld = null;

        public Image img_Screenshot = null;
        public TMP_Text lbl_Caption = null;

        public WorldInfo WorldInfo { get; internal set; } = null;
        public Cid WorldCid { get; internal set; } = null;
        public int ServersCount { get; internal set; } = 0;
        public int UsersCount { get; internal set; } = 0;
        public int FriendsMax { get; internal set; } = 0;
        public bool AllowedForThis {  get; internal set; } = true;

        private string patternCaption = null;

        [Obsolete("URL -> Cid conversion")]
        public static WorldPaneltem New(Transform parent, Cid cid)
        {
            GameObject go = Instantiate(Resources.Load<GameObject>("UI/Components/WorldListItem"));
            go.transform.SetParent(parent, false);
            WorldPaneltem worldListItem = go.GetComponent<WorldPaneltem>();
            worldListItem.WorldInfo.WorldCid = cid;
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

            PopulateWorldData(WorldInfo.WorldCid);
        }

        private void PopulateWorldData(Cid cid)
        {
            IEnumerator VisCoroutine()
            {
                yield return null;

                if (!string.IsNullOrEmpty(WorldInfo?.WorldName))
                    VisualizeWorldData();
                else
                    lbl_Caption.text = $"({cid})";
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

            btn_Add.gameObject.SetActive(!WorldInfo.IsFavourited());
            btn_Delete.gameObject.SetActive(WorldInfo.IsFavourited());


            if(WorldInfo.ScreenshotPNG != null)
                Utils.ShowImage(WorldInfo.ScreenshotPNG, img_Screenshot);


            string lvstr = (WorldInfo.Updated == DateTime.MinValue)
                ? "Never"
                : WorldInfo.Updated.ToShortDateString();

            lbl_Caption.text = string.Format(patternCaption,
                WorldInfo.WorldName,
                lvstr,
                ServersCount,
                UsersCount,
                FriendsMax);
        }

        private void OnVisitClicked(bool inPlace)
        {
            if(!string.IsNullOrEmpty(WorldInfo.WorldCid))
            {
                if(inPlace)
                    WorldTransition.EnterWorldAsync(WorldInfo.WorldCid);
                else
                    ServerSearcher.InitiateServerTransition(WorldInfo.WorldCid);

                WorldInfo.BumpWI();
            }
        }

        private void OnAddClicked()
        {
            WorldInfo.Favourite();
            PopulateWorldData(WorldInfo.WorldCid);
        }

        private void OnDeleteClicked()
        {
            WorldInfo.Unfavourite();
            PopulateWorldData(WorldInfo.WorldCid);
        }
    }
}
