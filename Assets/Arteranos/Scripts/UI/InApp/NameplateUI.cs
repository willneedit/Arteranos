/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Arteranos.Avatar;
using Arteranos.XR;
using Arteranos.Social;
using Arteranos.Core;
using System.Linq;

namespace Arteranos.UI
{
    public class NameplateUI : UIBehaviour, INameplateUI
    {
        public IAvatarBrain Bearer { get; set; } = null;

        [SerializeField] private TextMeshProUGUI lbl_Name = null;
        [SerializeField] private Button btn_mute = null;
        [SerializeField] private Button btn_unmute = null;
        [SerializeField] private Button btn_friend_add = null;
        [SerializeField] private Button btn_block = null;

        private readonly Vector3 aboutFace = new(0, 180, 0);

        protected override void Awake()
        {
            base.Awake();

            btn_mute.onClick.AddListener(OnMuteButtonClicked);
            btn_unmute.onClick.AddListener(OnMuteButtonClicked);
            btn_friend_add.onClick.AddListener(OnFriendAddButtonClicked);
            btn_block.onClick.AddListener(OnBlockButtonClicked);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            lbl_Name.text = GetLabelText();

            Bearer.OnAppearanceStatusChanged += OnAppearanceStatusChanged;
            float fullHeight = Bearer.Body.FullHeight;
            // TODO Up vector? LocalPosition respects the root rotation, but...
            transform.localPosition = new Vector3(0, fullHeight + 0.2f, 0);
            transform.localRotation = Quaternion.Euler(0, 180, 0);
            OnAppearanceStatusChanged(Bearer.AppearanceStatus);
        }

        protected override void OnDisable()
        {
            Bearer.OnAppearanceStatusChanged -= OnAppearanceStatusChanged;

            base.OnDisable();
        }

        private void Update()
        {
            // LookAt can take a second vector as the Up vector.
            // Seems that we'd use _the client's_ Up vector, not the targeted avatar's.
            transform.LookAt(XRControl.Instance.cameraTransform);
            transform.Rotate(aboutFace);
        }

        private string GetLabelText()
        {
            string tagstr = null;

            int yourstate = XRControl.Me.GetOwnState(Bearer);
            int hisstate = XRControl.Me.GetReflectiveState(Bearer);

            if(SocialState.IsState(yourstate, SocialState.Friend_bonded))
                tagstr = "Friend";

            // _Exactly_ the offering state.
            else if(SocialState.IsState(hisstate, SocialState.Friend_offered))
                tagstr = "Wants to be your friend";

            return tagstr == null
                ? Bearer.Nickname
                : $"{Bearer.Nickname}\n{tagstr}";
        }

        private void OnAppearanceStatusChanged(int status)
        {
            // Adding friends is visible if it's not already in progress
            int yourstate = XRControl.Me.GetOwnState(Bearer);
            bool friends = SocialState.IsState(yourstate, SocialState.Friend_offered);
            bool blocked = SocialState.IsState(yourstate, SocialState.Blocked);

            // No point of dealing with the blocked status - it isn't visible if it is blocked.
            btn_friend_add.gameObject.SetActive(!friends && !blocked);
            btn_block.gameObject.SetActive(!friends);

            //       Design: Speaker on if the audio status okay and not individually muted
            //               Speaker off it it's anything else
            // Interactable: Only if it's not self-muted and not gagged
            Button current = (!AppearanceStatus.IsSilent(status)) ? btn_mute : btn_unmute;

            btn_mute.gameObject.SetActive(current == btn_mute);
            btn_unmute.gameObject.SetActive(current == btn_unmute);

            // Only interactable if there's no other reason for it.
            current.interactable = (status & ~AppearanceStatus.Muted) == AppearanceStatus.OK;
        }

        private void OnMuteButtonClicked()
        {
            // Regular users can mute other users
            Bearer.AppearanceStatus ^= AppearanceStatus.Muted;

            // Elevated users can gag other users.
            // TODO User privileges management
            // TODO Client HUD UI - self-muting
            // Bearer.AudioStatus ^= Avatar.AudioStatus.Gagged;

            // Update the visible state
            OnAppearanceStatusChanged(Bearer.AppearanceStatus);
        }

        private void OnBlockButtonClicked()
        {
            XRControl.Me.BlockUser(Bearer);
            OnAppearanceStatusChanged(Bearer.AppearanceStatus);
        }

        private void OnFriendAddButtonClicked()
        {
            XRControl.Me.OfferFriendship(Bearer);
            OnAppearanceStatusChanged(Bearer.AppearanceStatus);
        }
    }
}
