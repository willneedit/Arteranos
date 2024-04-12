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
using Ipfs;
using System.IO;

namespace Arteranos.UI
{
    public class UserListItem : ListItemBase
    {
        [SerializeField] private TMP_Text lbl_caption = null;

        private HoverButton btn_AddFriend = null; // Offering Friend or accepting the request
        private HoverButton btn_DelFriend = null; // Revoking Friend offer or unfriend
        private HoverButton btn_Block = null; // Block user
        private HoverButton btn_Unblock= null; // Unblock user
        private HoverButton btn_SendText= null; // Message user

        public UserID targetUserID = null;

        private IAvatarBrain Me = null;
        private Client cs = null;

        public static UserListItem New(Transform parent, UserID targetUserID)
        {
            GameObject go = Instantiate(BP.I.UIComponents.UserListItem);
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
            btn_SendText= btns_ItemButton[4];

            btn_AddFriend.onClick.AddListener(OnAddFriendButtonClicked);
            btn_DelFriend.onClick.AddListener(OnDelFriendButtonClicked);
            btn_Block.onClick.AddListener(OnBlockButtonClicked);
            btn_Unblock.onClick.AddListener(OnUnblockButtonClicked);
            btn_SendText.onClick.AddListener(OnSendTextButtonClicked);

            Me = XRControl.Me;
            cs = SettingsManager.Client;
        }

        protected override void Start()
        {
            base.Start();

            lbl_caption.text = targetUserID;
        }

        private bool DownloadProgress = false;
        private void Update()
        {
            IEnumerator DownloadUserIcon(Cid icon)
            {
                Stream stream = null;
                yield return Utils.Async2Coroutine(IPFSService.ReadFile(icon), _stream => stream = _stream);

                // Intentionally left on. Don't bother with repeated requests for nonexistent icons.
                if (stream == null) yield break;

                using MemoryStream ms = new();
                yield return Utils.CopyWithProgress(stream, ms);
                byte[] data = ms.ToArray();

                Texture2D tex = null;
                yield return Utils.LoadImageCoroutine(data, _tex => tex = _tex);

                // Same as with broken image. 
                if (tex == null) yield break;

                // TODO image texture update

                // Finished. Now we _can_ update when necessary.
                DownloadProgress = false;
            }

            Cid Icon = null;
            IAvatarBrain targetUser = null;

            // When it's hovered, watch for the status updates - both internal and external causes.
            if (go_Overlay.activeSelf)
            {
                IEnumerable<KeyValuePair<UserID, UserSocialEntryJSON>> q = SettingsManager.Client.GetSocialList(targetUserID);
                
                ulong currentState = q.Any() ? q.First().Value.state : SocialState.None;
                Icon = q.Any() ? q.First().Value.Icon : null;

                bool friends = SocialState.IsFriendRequested(currentState);

                bool blocked = SocialState.IsBlocked(currentState);

                btn_AddFriend.gameObject.SetActive(!friends && !blocked);
                btn_DelFriend.gameObject.SetActive(friends && !blocked);

                btn_Block.gameObject.SetActive(!blocked && !friends);
                btn_Unblock.gameObject.SetActive(blocked && !friends);

                // Connot send texts to offline users. They could want to deny them.
                targetUser = NetworkStatus.GetOnlineUser(targetUserID);

                if (targetUser != null && XRControl.Me != null)
                    btn_SendText.gameObject.SetActive(Utils.IsAbleTo(UserCapabilities.CanSendText, targetUser));
                else
                    btn_SendText.gameObject.SetActive(false);

            }

            if (targetUser != null)
            {
                // TODO Online User's icon
                // Icon = targetUser.UserIconCID();
            }

            // Icons will be requested when it's saved or online users, but only hovered.
            if (Icon != null && !DownloadProgress /* && texture == null */)
            {
                DownloadProgress = true;
                StartCoroutine(DownloadUserIcon(Icon));
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

        private void OnSendTextButtonClicked()
        {
            IAvatarBrain targetUser = NetworkStatus.GetOnlineUser(targetUserID);
            if (targetUser == null)
            {
                IDialogUI dialog = DialogUIFactory.New();
                dialog.Text = "User is offline.";
                dialog.Buttons = new string[] { "OK" };
                return;
            }

            SysMenu.CloseSysMenus();
            TextMessageUIFactory.New(targetUser);
        }
    }
}
