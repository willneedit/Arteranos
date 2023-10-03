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

namespace Arteranos.UI
{
    public class ServerUserListUI : UIBehaviour
    {

        public RectTransform lvc_WorldList;

        public static ServerUserListUI New()
        {
            GameObject go = Instantiate(Resources.Load<GameObject>("UI/UI_ServerUserList"));
            return go.GetComponent<ServerUserListUI>();
        }

        protected override void Awake() => base.Awake();

        protected override void Start()
        {
            base.Start();

            // Offline, query directly from the local database.
            if(SettingsManager.CurrentServer == null)
            {
                foreach(ServerUserState item in SettingsManager.ServerUsers.FindUsers(new()))
                    PopulateSUBItem(item);
            }
            else XRControl.Me.QueryServerUserBase(PopulateSUBItem);

        }

        private void PopulateSUBItem(ServerUserState state) 
            => ServerUserListItem.New(lvc_WorldList, state);
    }
}
