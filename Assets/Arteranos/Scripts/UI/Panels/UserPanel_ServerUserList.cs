/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Arteranos.Core;
using Arteranos.XR;
using System;
using System.Collections.Generic;
using Arteranos.Avatar;
using Arteranos.Services;

namespace Arteranos.UI
{
    public class UserPanel_ServerUserList : UIBehaviour
    {

        public RectTransform lvc_ServerUserList;

        protected override void Awake() => base.Awake();

        protected override void OnEnable()
        {
            base.OnEnable();

            // Offline, query directly from the local database.
            if (XRControl.Me == null)
            {
                foreach (ServerUserState item in SettingsManager.ServerUsers.FindUsers(new()))
                    PopulateSUBItem(item);
            }
            else
            {
                // Online users first.
                foreach (IAvatarBrain user in NetworkStatus.GetOnlineUsers())
                    PopulateOnlineSUBItem(user);

                XRControl.Me.QueryServerUserBase(PopulateSUBItem);
            }
        }

        protected override void OnDisable()
        {
            List<GameObject> list = new();

            for (int i = 0, c = lvc_ServerUserList.transform.childCount; i < c; ++i)
                list.Add(lvc_ServerUserList.transform.GetChild(i).gameObject);

            foreach (GameObject item in list) Destroy(item);

            // Abort the server user database query if necessary.
            XRControl.Me?.QueryServerUserBase(null);

            base.OnDisable();
        }

        private void PopulateOnlineSUBItem(IAvatarBrain onlineUser)
        {
            // If it's there, you'll find it in the database.
            if (onlineUser.UserState != UserState.Normal) return;

            ServerUserState user = new()
            {
                userID = onlineUser.UserID,
                userState = onlineUser.UserState,
                remarks = string.Empty
            };
            PopulateSUBItem(user);
        }

        private void PopulateSUBItem(ServerUserState state) 
            => ServerUserListItem.New(lvc_ServerUserList, state);
    }
}
