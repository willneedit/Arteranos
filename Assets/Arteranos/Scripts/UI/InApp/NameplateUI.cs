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
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            lbl_Name.text = Bearer.Nickname;

            Bearer.OnNetMuteStatusChanged += OnNetMuteStatusChanged;
            float fullHeight = Bearer.Body.FullHeight;
            // TODO Up vector? LocalPosition respects the root rotation, but...
            transform.localPosition = new Vector3(0, fullHeight + 0.2f, 0);
            transform.localRotation = Quaternion.Euler(0, 180, 0);
            OnNetMuteStatusChanged(Bearer.NetMuteStatus);
        }

        protected override void OnDisable()
        {
            Bearer.OnNetMuteStatusChanged -= OnNetMuteStatusChanged;

            base.OnDisable();
        }

        private void Update()
        {
            // LookAt can take a second vector as the Up vector.
            // Seems that we'd use _the client's_ Up vector, not the targeted avatar's.
            transform.LookAt(XR.XRControl.Instance.cameraTransform);
            transform.Rotate(aboutFace);
        }

        private void OnNetMuteStatusChanged(int status)
        {
            //       Design: Speaker on if the audio status okay and not individually muted
            //               Speaker off it it's anything else
            // Interactable: Only if it's not self-muted and not gagged
            Button current = (status == 0 && !Bearer.ClientMuted) ? btn_mute : btn_unmute;

            btn_mute.gameObject.SetActive(current == btn_mute);
            btn_unmute.gameObject.SetActive(current == btn_unmute);

            current.interactable = (status == 0);
        }

        private void OnMuteButtonClicked()
        {
            // Regular users can mute other users
            Bearer.ClientMuted = !Bearer.ClientMuted;

            // Elevated users can gag other users.
            // TODO User privileges management
            // TODO Client HUD UI - self-muting
            // Bearer.AudioStatus ^= Avatar.AudioStatus.Gagged;

            // Update the visible state
            OnNetMuteStatusChanged(Bearer.NetMuteStatus);
        }

    }
}
