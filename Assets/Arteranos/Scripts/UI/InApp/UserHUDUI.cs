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
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Arteranos.Core;

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
        [SerializeField] private RectTransform EmojiFlyout;
        [SerializeField] private TMP_Text ToolTipText;
        [SerializeField] private Material Material;

        // Must match the ordering in the array, not necessarily the ordering in the UI
        private const int btn_mute = 0;
        private const int btn_unmute = 1;
        private const int btn_callcd = 2;
        private const int btn_disconnect = 3;
        private const int btn_emotes = 4;
        private const int btn_takephoto = 5;
        private const int btn_dismisscd = 6;
        private UnityAction[] Actions;

        private bool cameraCalled = false;

        protected override void Awake()
        {
            base.Awake();

            Actions = new UnityAction[]
            {
                () => XRControl.Me.AppearanceStatus |= AppearanceStatus.Muting,
                () => XRControl.Me.AppearanceStatus &= ~AppearanceStatus.Muting,
                OnSummonCameraClicked,
                () => NetworkStatus.StopHost(true),
                () => StartCoroutine(ToggleFlyout(EmojiFlyout)),
                OnTakePhotoClicked,
                OnDismissCameraClicked
            };
        }

        protected override void Start()
        {
            Action<bool> makeHoverTip(string hoverTip) =>
                (x) => ToolTipText.text = hoverTip;

            UnityAction makeClickedEmoji(EmojiButton but) =>
                () => XRControl.Me.PerformEmote(but.Image.name);

            base.Start();

            ToolTipText.text = " "; // Empty, but still taking some vertical space.

            SystemMenuButton.Button.onHover += makeHoverTip(SystemMenuButton.HoverTip);
            SystemMenuButton.Button.onClick.AddListener(SysMenu.OpenSysMenu);

            for(int i = 0; i < HUDButtons.Length; i++)
            {
                HUDButton HudButton = HUDButtons[i];
                HudButton.Button.onHover += makeHoverTip(HudButton.HoverTip);
                HudButton.Button.onClick.AddListener(Actions[i]);
            }

            GameObject sampleButton = EmojiFlyout.transform.GetChild(0).gameObject;

            int emotes = 0;
            foreach(EmojiButton emojiButton in EmojiSettings.Load().EnumerateEmotes())
            {
                // TODO Redirection with the favourite emotes preferences panel.
                // Only for the first sixteen emotes in the set order
                if(emotes++ >= 16) break;

                Texture2D image = emojiButton.Image;

                GameObject go = Instantiate(sampleButton, EmojiFlyout);

                HoverButton eb = go.GetComponent<HoverButton>();

                eb.onHover += makeHoverTip(emojiButton.HoverTip);

                eb.image.sprite = Sprite.Create(
                    image,
                    new Rect(0, 0, image.width, image.height),
                    Vector2.zero);
                eb.name = emojiButton.Image.name;
                eb.onClick.AddListener(makeClickedEmoji(emojiButton));

                go.SetActive(true);
            }
        }

        public bool lateInitialized = false;

        private void Update()
        {
            if(!lateInitialized)
            {
                if(SettingsManager.Client != null)
                {
                    SettingsManager.Client.OnUserHUDSettingsChanged += DownloadUserHUDSettings;
                    SettingsManager.Client.PingUserHUDChanged();
                    lateInitialized= true;
                }
            }

            bool avatarOn = XRControl.Me != null;
            bool muted = AppearanceStatus.IsSilent(XRControl.Me?.AppearanceStatus ?? AppearanceStatus.OK);

            // Safety measure, to accidentally shut down a hosted session.
//            bool online = NetworkStatus.GetOnlineLevel() == OnlineLevel.Client;
            bool online = NetworkStatus.GetOnlineLevel() != OnlineLevel.Offline;

            HUDButtons[btn_mute].Button.gameObject.SetActive(avatarOn && !muted);
            HUDButtons[btn_unmute].Button.gameObject.SetActive(avatarOn && muted);
            HUDButtons[btn_emotes].Button.gameObject.SetActive(avatarOn);

            HUDButtons[btn_disconnect].Button.gameObject.SetActive(online);

            HUDButtons[btn_callcd].Button.gameObject.SetActive(!cameraCalled);
            HUDButtons[btn_takephoto].Button.gameObject.SetActive(cameraCalled);
            HUDButtons[btn_dismisscd].Button.gameObject.SetActive(cameraCalled);
        }

        private void DownloadUserHUDSettings(UserHUDSettingsJSON obj)
        {
            CameraUITracker ct = GetComponent<CameraUITracker>();
            RectTransform rt = GetComponent<RectTransform>();

            ct.m_offset = new Vector3(
                obj.AxisX / 10.0f,
                obj.AxisY / 10.0f,
                0.25f);

            rt.localScale = new Vector3(
                Mathf.Pow(2, obj.Log2Size) * 0.001f,
                Mathf.Pow(2, obj.Log2Size) * 0.001f,
                1);

            ct.m_Delay = obj.Delay;

            ct.m_Tolerance = obj.Tightness;
        }

        private IEnumerator ToggleFlyout(RectTransform rt)
        {
            float duration = 0.25f;

            float elapsed = 0.0f;
            float sourcescale = rt.localScale.x;
            float targetScale = (sourcescale == 0) ? 1 : 0;

            while(elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalizedProgress = elapsed / duration;

                float currentScale = Mathf.Lerp(sourcescale, targetScale, normalizedProgress);

                rt.localScale = new Vector2(currentScale, 1);

                yield return new WaitForEndOfFrame();
            }
        }

        private void OnSummonCameraClicked()
        {
            SysMenu.DismissGadget(SysMenu.GADGET_CAMERA_DRONE);

            GameObject go = Instantiate(Resources.Load<GameObject>("UI/InApp/CameraDrone"));

            // In front of yourself
            Transform view = Camera.main.transform;
            Vector3 inFront = view.position + (view.rotation * new Vector3(0, -0.5f, 1));
            go.transform.SetPositionAndRotation(inFront, view.rotation);

            cameraCalled = true;
        }

        private void OnDismissCameraClicked()
        {
            SysMenu.DismissGadget(SysMenu.GADGET_CAMERA_DRONE);
            cameraCalled = false;
        }

        private void OnTakePhotoClicked()
        {
            CameraDroneUI cdui = SysMenu.FindGadget(SysMenu.GADGET_CAMERA_DRONE).GetComponent<CameraDroneUI>();

            cdui.TakePhoto();
        }
    }
}
