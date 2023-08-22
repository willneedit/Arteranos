/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Arteranos.Avatar;
using Arteranos.XR;
using Arteranos.Core;
using System.Linq;
using Arteranos.Social;
using TMPro;
using Arteranos.Services;

namespace Arteranos.UI
{
    public class UserListItem : ListItemBase
    {
        [SerializeField] private TMP_Text lbl_caption = null;

        private HoverButton btn_AddFriend = null; // Offering Friend or accepting the request
        private HoverButton btn_DelFriend = null; // Revoking Friend offer or unfriend
        private HoverButton btn_Block = null; // Block user
        private HoverButton btn_Unblock= null; // Unblock user

        public UserID targetUserID = null;

        private IAvatarBrain Me = null;
        private ClientSettings cs = null;

        public static UserListItem New(Transform parent, UserID targetUserID)
        {
            GameObject go = Instantiate(Resources.Load<GameObject>("UI/Components/UserListItem"));
            go.transform.SetParent(parent, false);
            UserListItem UserListItem = go.GetComponent<UserListItem>();
            UserListItem.targetUserID = targetUserID;
            return UserListItem;
        }

        protected override void Awake()
        {
            base.Awake();

            btn_AddFriend = btns_ItemButton[0];
            btn_DelFriend= btns_ItemButton[1];
            btn_Block= btns_ItemButton[2];
            btn_Unblock= btns_ItemButton[3];

            btn_AddFriend.onClick.AddListener(OnAddFriendButtonClicked);
            btn_DelFriend.onClick.AddListener(OnDelFriendButtonClicked);
            btn_Block.onClick.AddListener(OnBlockButtonClicked);
            btn_Unblock.onClick.AddListener(OnUnblockButtonClicked);

            Me = XRControl.Me;
            cs = SettingsManager.Client;
        }

        protected override void Start()
        {
            base.Start();

            lbl_caption.text = targetUserID;
        }

        private void Update()
        {
            // When it's active, watch for the status updates - both internal and external causes.
            if(go_Overlay.activeSelf)
            {
                IEnumerable<SocialListEntryJSON> q = SettingsManager.Client.GetSocialList(targetUserID);

                ulong currentState = (q.Count() > 0) ? q.First().State : SocialState.None;

                bool friends = SocialState.IsFriendRequested(currentState);

                bool blocked = SocialState.IsBlocked(currentState);

                btn_AddFriend.gameObject.SetActive(!friends && !blocked);
                btn_DelFriend.gameObject.SetActive(friends && !blocked);

                btn_Block.gameObject.SetActive(!blocked && !friends);
                btn_Unblock.gameObject.SetActive(blocked && !friends);
            }
        }

        private void OnAddFriendButtonClicked()
        {
            IAvatarBrain targetUser = NetworkStatus.GetOnlineUser(targetUserID);
            if(targetUser != null)
            {
                Me.OfferFriendship(targetUser, true);
                return;
            }

            cs.UpdateSocialListEntry(targetUserID, (x) =>
            {
                ulong state = x;
                SocialState.SetFriendState(ref state, true);
                return state;
            });
        }

        private void OnDelFriendButtonClicked()
        {
            IAvatarBrain targetUser = NetworkStatus.GetOnlineUser(targetUserID);
            if(targetUser != null)
            {
                Me.OfferFriendship(targetUser, false);
                return;
            }

            cs.UpdateSocialListEntry(targetUserID, (x) =>
            {
                ulong state = x;
                SocialState.SetFriendState(ref state, false);
                return state;
            });
        }

        private void OnBlockButtonClicked()
        {
            IAvatarBrain targetUser = NetworkStatus.GetOnlineUser(targetUserID);
            if(targetUser != null)
            {
                Me.BlockUser(targetUser, true);
                return;
            }

            cs.UpdateSocialListEntry(targetUserID, (x) =>
            {
                ulong state = x;
                SocialState.SetBlockState(ref state, true);
                return state;
            });
        }

        private void OnUnblockButtonClicked()
        {
            IAvatarBrain targetUser = NetworkStatus.GetOnlineUser(targetUserID);
            if(targetUser != null)
            {
                Me.BlockUser(targetUser, false);
                return;
            }
            cs.UpdateSocialListEntry(targetUserID, (x) =>
            {
                ulong state = x;
                SocialState.SetBlockState(ref state, false);
                return state;
            });
        }
    }
}
