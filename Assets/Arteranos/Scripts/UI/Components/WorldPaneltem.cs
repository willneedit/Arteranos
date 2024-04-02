/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections;
using TMPro;
using UnityEngine.UI;

using Arteranos.Core;
using Arteranos.Services;
using Ipfs;
using Arteranos.Core.Operations;
using UnityEngine;

namespace Arteranos.UI
{
    public class WorldPaneltem : ListItemBase
    {
        private HoverButton btn_Add = null;
        private HoverButton btn_Visit = null;
        private HoverButton btn_Delete = null;
        private HoverButton btn_ChangeWorld = null;

        public RawImage img_Screenshot = null;
        public TMP_Text lbl_Caption = null;

        public Cid WorldCid { get; internal set; } = null;
        public int ServersCount { get; internal set; } = 0;
        public int UsersCount { get; internal set; } = 0;
        public int FriendsMax { get; internal set; } = 0;
        public bool Favourited { get; internal set; } = false;

        private bool AllowedForThis = true;
        private WorldInfo WorldInfo = null;
        private string patternCaption = null;

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

            PopulateWorldData();
        }

        private void PopulateWorldData()
        {
            IEnumerator VisCoroutine()
            {
                // If the database lookup failed, we have to look further...
                if (WorldInfo == null)
                    yield return WorldInfo.RetrieveCoroutine(WorldCid, (wi) => WorldInfo = wi);

                // Cid seems to be invalid, or expired, or unreachable.
                if (WorldInfo == null)
                {
                    lbl_Caption.text = "(unavailable)";
                    yield break;
                }

                ServerPermissions permission = WorldInfo.win.ContentRating;
                AllowedForThis = permission != null && !permission.IsInViolation(SettingsManager.ActiveServerData.Permissions);

                yield return VisualizeWorldData();
            }

            if(WorldCid == null)
            {
                lbl_Caption.text = "(deleted)";
                return;
            }

            WorldInfo = WorldInfo.DBLookup(WorldCid);

            StartCoroutine(VisCoroutine());
        }

        private IEnumerator VisualizeWorldData()
        {
            // If we're in Host mode, you're the admin of your own server, so we're able to
            // change the world. And you still have the great responsibility...
            btn_Visit.gameObject.SetActive(NetworkStatus.GetOnlineLevel() != OnlineLevel.Host);
            btn_ChangeWorld.gameObject.SetActive(
                Utils.IsAbleTo(Social.UserCapabilities.CanInitiateWorldTransition, null)
                && AllowedForThis);

            btn_Add.gameObject.SetActive(!Favourited);
            btn_Delete.gameObject.SetActive(Favourited);

            string lvstr = (WorldInfo.Updated == DateTime.MinValue)
                ? "Never"
                : WorldInfo.Updated.ToShortDateString();

            lbl_Caption.text = string.Format(patternCaption,
                WorldInfo.WorldName,
                lvstr,
                ServersCount,
                UsersCount,
                FriendsMax);

            if (WorldInfo.win.ScreenshotPNG != null)
                yield return Utils.LoadImageCoroutine(WorldInfo.win.ScreenshotPNG, _tex => img_Screenshot.texture = _tex);

            yield return null;
        }

        private void OnVisitClicked(bool inPlace)
        {
            if(!string.IsNullOrEmpty(WorldCid))
            {
                if (inPlace)
                    SettingsManager.EnterWorld(WorldCid);
                else
                    ServerSearcher.InitiateServerTransition(WorldCid);

                WorldInfo.BumpWI();
            }
        }

        private void OnAddClicked()
        {
            WorldInfo.Favourite();
            Favourited = true;
            PopulateWorldData();
        }

        private void OnDeleteClicked()
        {
            WorldInfo.Unfavourite();
            Favourited = false;
            WorldInfo.DBDelete(WorldCid);
            WorldCid = null;
            PopulateWorldData();
        }
    }
}
