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
using System.Collections.Generic;

namespace Arteranos.UI
{
    public class ServerUserBaseListItem : ListItemBase
    {
        private HoverButton btn_Ban = null;
        private HoverButton btn_Unban = null;
        private HoverButton btn_Promote = null;
        private HoverButton btn_Demote = null;

        public ServerUserState user;
        public Image img_Screenshot = null;
        public TMP_Text lbl_Caption = null;

        public static ServerUserBaseListItem New(Transform parent, ServerUserState user)
        {
            GameObject go = Instantiate(Resources.Load<GameObject>("UI/Components/ServerUserBaseListItem"));
            go.transform.SetParent(parent, false);
            ServerUserBaseListItem userListItem = go.GetComponent<ServerUserBaseListItem>();
            userListItem.user = user;
            return userListItem;
        }



        protected override void Awake()
        {
            base.Awake();

            btn_Ban = btns_ItemButton[0];
            btn_Unban = btns_ItemButton[1];
            btn_Promote = btns_ItemButton[2];
            btn_Demote = btns_ItemButton[4];


            btn_Ban.onClick.AddListener(OnBanClicked);
            btn_Unban.onClick.AddListener(OnUnbanClicked);
            btn_Promote.onClick.AddListener(OnPromoteClicked);
            btn_Demote.onClick.AddListener(OnDemoteClicked);
        }

        protected override void Start()
        {
            base.Start();

            UpdateUserData();
        }

        private void UpdateUserData()
        {
            (string idline, string statelist) = PopulateUserData(user);

            lbl_Caption.text = $"{idline}\n{statelist}";
        }

        private static (string, string) PopulateUserData(ServerUserState user)
        {
            static string Elaborate(ServerUserState user)
            {
                if (!string.IsNullOrEmpty(user.remarks))
                    return $"Banned ({user.remarks})";

                string reason = KickBanUI.FindBanReason(user.userState);

                return $"Banned ({reason ?? "unknown"})";
            }

            string idlinefmt = "ID: {0] Address: {1}, Device ID: {2}";

            string userID = user.userID ?? "<unset>";
            string address = user.address ?? "<unset>";
            string deviceUID = user.deviceUID != null ? user.deviceUID[0..9] : "<unset>";

            List<string> states = new();

            if (UserState.IsBanned(user.userState)) states.Add(Elaborate(user));
            if (Bit64field.IsAny(user.userState, UserState.Srv_admin)) states.Add("Server Admin");
            if (Bit64field.IsAny(user.userState, UserState.Srv_admin_asstnt)) states.Add("Deputy Server Admin");

            string statelist = string.Join(", ", states);
            string idline = string.Format(idlinefmt, userID, address, deviceUID);
            return (idline, statelist);
        }


        private void OnBanClicked()
        {
            throw new NotImplementedException();
        }

        private void OnUnbanClicked()
        {
            throw new NotImplementedException();
        }

        private void OnPromoteClicked()
        {
            throw new NotImplementedException();
        }

        private void OnDemoteClicked()
        {
            throw new NotImplementedException();
        }
    }
}
