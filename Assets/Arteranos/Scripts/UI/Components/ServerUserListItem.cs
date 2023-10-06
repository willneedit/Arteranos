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
using Arteranos.XR;

namespace Arteranos.UI
{
    public class ServerUserListItem : ListItemBase
    {
        private HoverButton btn_Unban = null;
        private HoverButton btn_Promote = null;
        private HoverButton btn_Demote = null;

        public ServerUserState user;
        public Image img_Screenshot = null;
        public TMP_Text lbl_Caption = null;

        public static ServerUserListItem New(Transform parent, ServerUserState user)
        {
            GameObject go = Instantiate(Resources.Load<GameObject>("UI/Components/ServerUserListItem"));
            go.transform.SetParent(parent, false);
            ServerUserListItem userListItem = go.GetComponent<ServerUserListItem>();
            userListItem.user = user;
            return userListItem;
        }



        protected override void Awake()
        {
            base.Awake();

            btn_Unban = btns_ItemButton[0];
            btn_Promote = btns_ItemButton[1];
            btn_Demote = btns_ItemButton[2];


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

            bool IsBanned = UserState.IsBanned(user.userState);
            bool isSrvAdminAsstnt = Bit64field.IsAny(user.userState, UserState.Srv_admin_asstnt);
            bool canEditUsers = Utils.IsAbleTo(Social.UserCapabilities.CanAdminServerUsers, null);

            btn_Unban.gameObject.SetActive(IsBanned && canEditUsers);

            // User in question could not be a server admin of any kind
            btn_Promote.gameObject.SetActive(!UserState.IsSAdmin(user.userState) && !IsBanned && canEditUsers);

            // if it's a deputy server admin it's okay to self-demote.
            btn_Demote.gameObject.SetActive(isSrvAdminAsstnt && canEditUsers);

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

            string userID = user.userID ?? "<unset>";
            string address = user.address ?? "<unset>";
            string deviceUID = user.deviceUID != null ? user.deviceUID[0..9] : "<unset>";

            List<string> states = new();

            if (UserState.IsBanned(user.userState)) states.Add(Elaborate(user));
            if (Bit64field.IsAny(user.userState, UserState.Srv_admin)) states.Add("Server Admin");
            if (Bit64field.IsAny(user.userState, UserState.Srv_admin_asstnt)) states.Add("Deputy Server Admin");

            string statelist = string.Join(", ", states);
            string idline = string.Format("ID: {0}, Address: {1}, Device ID: {2}", userID, address, deviceUID);
            return (idline, statelist);
        }

        private void UpdateServerUserState()
        {
            // Offline, modifying local installation
            if(XRControl.Me == null)
            {
                ServerUserBase sub = SettingsManager.ServerUsers;

                sub.RemoveUsers(user);
                sub.AddUser(user);
                sub.Save();
            }
            else
                XRControl.Me.UpdateServerUserState(user);

            UpdateUserData();
        }

        private void OnUnbanClicked()
        {
            user.userState &= ~UserState.Banned;

            UpdateServerUserState();
        }

        private void OnPromoteClicked()
        {
            user.userState |= UserState.Srv_admin_asstnt;

            UpdateServerUserState();
        }

        private void OnDemoteClicked()
        {
            user.userState &= ~UserState.Srv_admin_asstnt;

            UpdateServerUserState();
        }
    }
}
