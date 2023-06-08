/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Arteranos.Core;

namespace Arteranos.UI
{
    public abstract class FriendPanelBase : UIBehaviour
    {
        public RectTransform lvc_UserList;

        protected ClientSettings cs = null;

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            cs = SettingsManager.Client;

            if(cs == null) return;

            foreach(var entry in GetSocialListTab())
                UserListItem.New(lvc_UserList.transform, entry.UserID, entry.Nickname);
        }

        protected override void OnDisable()
        {
            while(lvc_UserList.transform.childCount > 0)
                Destroy(lvc_UserList.transform.GetChild(0).gameObject);

            base.OnDisable();
        }

        public abstract IEnumerable<SocialListEntryJSON> GetSocialListTab();
    }
}
