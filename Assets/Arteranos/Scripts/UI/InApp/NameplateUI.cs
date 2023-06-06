/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System;
using Arteranos.Avatar;
using Arteranos.XR;

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

            lbl_Name.text = Bearer.Nickname;

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
            transform.LookAt(XR.XRControl.Instance.cameraTransform);
            transform.Rotate(aboutFace);
        }

        private void OnAppearanceStatusChanged(int status)
        {
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

        private void OnBlockButtonClicked() => XRControl.Me.BlockUser(Bearer);

        private void OnFriendAddButtonClicked() => XRControl.Me.OfferFriendship(Bearer);
    }
}
