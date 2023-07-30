/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Avatar;
using Arteranos.Services;
using Arteranos.XR;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace Arteranos.UI
{
    [Serializable]
    internal struct HUDButton
    {
        public HoverButton Button;
        public string HoverTip;
    }

    public class UserHUDUI : UIBehaviour
    {
        [SerializeField] private HUDButton SystemMenuButton;
        [SerializeField] private HUDButton[] HUDButtons;
        [SerializeField] private TMP_Text ToolTipText;

        // Must match the ordering in the array, not necessarily the ordering in the UI
        private const int btn_mute = 0;
        private const int btn_unmute = 1;
        private const int btn_screenshot = 2;
        private const int btn_disconnect = 3;
        private const int btn_emotes = 4;

        private UnityAction[] Actions;

        protected override void Awake()
        {
            base.Awake();

            Actions = new UnityAction[]
            {
                OnMuteClicked,
                OnUnmuteClicked,
                OnScreenshotClicked,
                OnDisconnectClicked,
                OnEmotesClicked
            };
        }

        protected override void Start()
        {
            Action<bool> makeHoverTip(string hoverTip) =>
                (x) => ToolTipText.text = hoverTip;

            base.Start();

            SystemMenuButton.Button.onHover += makeHoverTip(SystemMenuButton.HoverTip);
            SystemMenuButton.Button.onClick.AddListener(OnSysMenuClicked);

            for(int i = 0; i < HUDButtons.Length; i++)
            {
                HUDButton HudButton = HUDButtons[i];
                HudButton.Button.onHover += makeHoverTip(HudButton.HoverTip);
                HudButton.Button.onClick.AddListener(Actions[i]);
            }

            NetworkStatus.OnNetworkStatusChanged += (_1, _2) => UpdateHUD();

            UpdateHUD();
        }

        private void UpdateHUD()
        {
            bool avatarOn = XRControl.Me != null;
            bool muted = AppearanceStatus.IsSilent(XRControl.Me?.AppearanceStatus ?? AppearanceStatus.OK);

            // Safety measure, to accidentally shut down a hosted session.
//            bool online = NetworkStatus.GetOnlineLevel() == OnlineLevel.Client;
            bool online = NetworkStatus.GetOnlineLevel() != OnlineLevel.Offline;

            HUDButtons[btn_mute].Button.gameObject.SetActive(avatarOn && !muted);
            HUDButtons[btn_unmute].Button.gameObject.SetActive(avatarOn && muted);

            HUDButtons[btn_disconnect].Button.gameObject.SetActive(online);
        }

        private void OnSysMenuClicked() => SysMenu.OpenSysMenu();

        private void OnMuteClicked()
        {
            XRControl.Me.AppearanceStatus |= AppearanceStatus.Muting;
            UpdateHUD();
        }

        private void OnUnmuteClicked()
        {
            XRControl.Me.AppearanceStatus &= ~AppearanceStatus.Muting;
            UpdateHUD();
        }

        private void OnScreenshotClicked() => throw new NotImplementedException();
        private void OnDisconnectClicked()
        {
            NetworkStatus.StopHost(true);
            UpdateHUD();
        }

        private void OnEmotesClicked() => throw new NotImplementedException();

    }
}
