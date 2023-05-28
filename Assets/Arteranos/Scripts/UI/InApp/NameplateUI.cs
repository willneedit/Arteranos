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

namespace Arteranos
{
    public class NameplateUI : UIBehaviour
    {
        public Avatar.IAvatarBrain Bearer { get; private set; } = null;

        [SerializeField] private TextMeshProUGUI lbl_Name = null;
        [SerializeField] private Button btn_mute = null;
        [SerializeField] private Button btn_unmute = null;
        [SerializeField] private Button btn_friend_add = null;
        [SerializeField] private Button btn_block = null;

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
            OnNetMuteStatusChanged(Bearer.NetMuteStatus);
        }

        protected override void OnDisable()
        {
            Bearer.OnNetMuteStatusChanged -= OnNetMuteStatusChanged;

            base.OnDisable();
        }

        private void OnNetMuteStatusChanged(int status)
        {
            //       Design: Speaker on if the audio status okay and not individually muted
            //               Speaker off it it's anything else
            // Interactable: Only if it's not self-muted and not gagged
            Button current = (status == 0 && !Bearer.ClientMuted) ? btn_mute : btn_unmute;

            btn_mute.gameObject.SetActive(false);
            btn_unmute.gameObject.SetActive(false);

            current.gameObject.SetActive(true);
            
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
        }

    }
}
