/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Arteranos.Core;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;

namespace Arteranos.UI
{
    public class PrefPanel_Moderation : UIBehaviour
    {
        public TMP_InputField txt_ServerName = null;
        public TMP_InputField txt_ServerPort = null;
        public TMP_InputField txt_VoicePort = null;
        public TMP_InputField txt_MetdadataPort = null;

        public TMP_InputField txt_Description = null;
        public Button btn_Icon = null;
        public Image img_IconImage = null;
        public TMP_InputField txt_IconURL = null;

        public Button btn_WorldGallery = null;
        public Button btn_ContentPermissions = null;

        public Toggle chk_Guests = null;
        public Toggle chk_CustomAvatars = null;
        public Toggle chk_Flying = null;

        private ServerSettings ss = null;

        private bool dirty = false;

        protected override void Awake()
        {
            base.Awake();

            txt_ServerName.onValueChanged.AddListener(SetDirty);
            txt_Description.onValueChanged.AddListener(SetDirty);

            txt_ServerPort.onValueChanged.AddListener(SetDirty);
            //txt_ServerPort.onValidateInput += OnValidatePort;

            txt_VoicePort.onValueChanged.AddListener(SetDirty);
            //txt_VoicePort.onValidateInput += OnValidatePort;

            txt_MetdadataPort.onValueChanged.AddListener(SetDirty);
            //txt_MetdadataPort.onValidateInput += OnValidatePort;

            btn_Icon.onClick.AddListener(OnIconClicked);

            btn_WorldGallery.onClick.AddListener(OnWorldGalleryClicked);
            btn_ContentPermissions.onClick.AddListener(OnContentPermissionsClicked);

            chk_CustomAvatars.onValueChanged.AddListener(SetDirty);
            chk_Flying.onValueChanged.AddListener(SetDirty);
            chk_Guests.onValueChanged.AddListener(SetDirty);
        }

        //private char OnValidatePort(string text, int charIndex, char addedChar)
        //{
        //    if(addedChar < '0' || addedChar > '9') return '\0';

        //    return addedChar;
        //}

        private void SetDirty(bool _) => dirty = true;
        private void SetDirty(string _) => dirty = true;

        protected override void Start()
        {
            base.Start();

            ss = SettingsManager.Server;

            txt_ServerPort.text = ss.ServerPort.ToString();
            txt_VoicePort.text = ss.VoicePort.ToString();
            txt_MetdadataPort.text = ss.MetadataPort.ToString();

            txt_ServerName.text = ss.Name;
            txt_Description.text = ss.Description;

            if(ss.Icon != null && ss.Icon.Length != 0)
                UpdateIcon(ss.Icon);

            chk_Guests.isOn = ss.Permissions.Guests ?? false;
            chk_CustomAvatars.isOn = ss.Permissions.CustomAvatars ?? false;
            chk_Flying.isOn = ss.Permissions.Flying ?? true;

            // Reset the state as it's the initial state, not the blank slate.
            dirty = false;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if(ss != null)
            {
                // Only when the world loading is committed, not only the entry of the URL.
                // ss.WorldURL = txt_WorldURL.text;

                ss.ServerPort = int.Parse(txt_ServerPort.text);
                ss.VoicePort = int.Parse(txt_VoicePort.text);
                ss.MetadataPort = int.Parse(txt_MetdadataPort.text);

                ss.Name = txt_ServerName.text;
                ss.Description = txt_Description.text;

                ss.Permissions.Flying = chk_Flying.isOn;
                ss.Permissions.CustomAvatars = chk_CustomAvatars.isOn;
                ss.Permissions.Guests = chk_Guests.isOn;
            }

            // Might be to disabled before it's really started, so cs may be null yet.
            if(dirty) ss?.SaveSettings();
            dirty = false;
        }

        private void OnWorldGalleryClicked()
        {
            SysMenuKind.CloseSystemMenus();

            WorldListUI.New();
        }

        private void OnContentPermissionsClicked()
        {
            SysMenuKind.CloseSystemMenus();

            ContentFilterUI cui = ContentFilterUI.New();

            cui.spj = SettingsManager.Server.Permissions;

            cui.OnFinishConfiguring +=
                () => SettingsManager.Server?.SaveSettings();
        }

        private void OnIconClicked()
        {
            IEnumerator GetTexture(string iconURL)
            {
                UnityWebRequest www = UnityWebRequestTexture.GetTexture(iconURL);
                yield return www.SendWebRequest();

                if(www.result == UnityWebRequest.Result.Success)
                {
                    DownloadHandler dh = www.downloadHandler;
                    byte[] data = dh.nativeData.ToArray();

                    Texture2D tex = new(2, 2);
                    ImageConversion.LoadImage(tex, data);

                    if(tex.width > 512 || tex.height > 512)
                        yield break;

                    if(tex.width < 128 || tex.height < 128)
                        yield break;

                    ss.Icon = data;
                    UpdateIcon(data, tex);
                    dirty = true;
                }
                else
                {
                    Debug.Log(www.error);
                }
            }

            StartCoroutine(GetTexture(txt_IconURL.text));
        }

        private void UpdateIcon(byte[] data, Texture2D tex = null)
        {
            tex = tex != null ? tex : new(2, 2);

            ImageConversion.LoadImage(tex, data);
            img_IconImage.sprite = Sprite.Create(tex,
                new Rect(0, 0, tex.width, tex.height),
                Vector2.zero);
        }
    }
}
