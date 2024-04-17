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
using System.IO;
using Arteranos.Services;
using System.Collections;
using Ipfs;
using System;

namespace Arteranos.UI
{
    public class PrefPanel_Moderation : UIBehaviour
    {
        public TMP_InputField txt_ServerName = null;
        public TMP_InputField txt_ServerPort = null;
        public TMP_InputField txt_MetdadataPort = null;

        public TMP_InputField txt_Description = null;
        public IconSelectorBar bar_IconSelector = null;

        public Button btn_WorldGallery = null;
        public Button btn_ContentPermissions = null;

        public Toggle chk_Guests = null;
        public Toggle chk_CustomAvatars = null;
        public Toggle chk_Flying = null;

        public Toggle chk_Public = null;

        public Button btn_ClearCaches = null;

        private Cid ServerIcon = null;
        private ServerPermissions Permissions = null;

        private bool dirty = false;

        protected override void Awake()
        {
            base.Awake();

            txt_ServerName.onValueChanged.AddListener(SetDirty);
            txt_Description.onValueChanged.AddListener(SetDirty);

            txt_ServerPort.onValueChanged.AddListener(SetDirty);
            //txt_ServerPort.onValidateInput += OnValidatePort;

            txt_MetdadataPort.onValueChanged.AddListener(SetDirty);
            //txt_MetdadataPort.onValidateInput += OnValidatePort;

            chk_Public.onValueChanged.AddListener(SetDirty);

            btn_WorldGallery.onClick.AddListener(OnWorldGalleryClicked);
            btn_ContentPermissions.onClick.AddListener(OnContentPermissionsClicked);

            chk_CustomAvatars.onValueChanged.AddListener(SetDirty);
            chk_Flying.onValueChanged.AddListener(SetDirty);
            chk_Guests.onValueChanged.AddListener(SetDirty);

            btn_ClearCaches.onClick.AddListener(OnClearCachesClicked);

            bar_IconSelector.OnIconChanged += Bar_IconSelector_OnIconChanged;
        }

        private void Bar_IconSelector_OnIconChanged(byte[] obj)
        {
            IEnumerator UploadIcon(byte[] data)
            {
                using MemoryStream ms = new(obj);
                ms.Position = 0;
                yield return Utils.Async2Coroutine(IPFSService.AddStream(ms), _fsn => ServerIcon = _fsn.Id);

                dirty = true;
            }

            StartCoroutine(UploadIcon(obj));
        }

        //private char OnValidatePort(string text, int charIndex, char addedChar)
        //{
        //    if(addedChar < '0' || addedChar > '9') return '\0';

        //    return addedChar;
        //}

        private void SetDirty(bool _) => dirty = true;
        private void SetDirty(string _) => dirty = true;

        protected override void OnEnable()
        {
            base.OnEnable();

            SettingsManager.OnClientReceivedServerConfigAnswer += UpdateServerConfigDisplay;

            // Send the query.
            SettingsManager.EmitToServerCTSPacket(new CTSServerConfig()
            {
                config = null
            });
        }

        private void UpdateServerConfigDisplay(ServerJSON ss)
        {
            IEnumerator DownloadIcon(Cid icon)
            {
                yield return Utils.DownloadDataCoroutine(icon, _data => bar_IconSelector.IconData = _data);
                bar_IconSelector.TriggerUpdate();
            }

            ServerIcon = ss.ServerIcon;
            Permissions = ss.Permissions;

            txt_ServerPort.text = ss.ServerPort.ToString();
            txt_MetdadataPort.text = ss.MetadataPort.ToString();

            txt_ServerName.text = ss.Name;
            txt_Description.text = ss.Description;

            chk_Flying.isOn = ss.Permissions.Flying ?? true;

            chk_Public.isOn = ss.Public;

            StartCoroutine(DownloadIcon(ss.ServerIcon));

            dirty = false;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            SettingsManager.OnClientReceivedServerConfigAnswer -= UpdateServerConfigDisplay;

            if (!dirty) return;
            dirty = false;

            ServerJSON ss = new();

            ss.ServerIcon = ServerIcon;
            ss.Permissions = Permissions;

            ss.ServerPort = int.Parse(txt_ServerPort.text);
            ss.MetadataPort = int.Parse(txt_MetdadataPort.text);

            ss.Name = txt_ServerName.text;
            ss.Description = txt_Description.text;

            ss.Permissions.Flying = chk_Flying.isOn;

            ss.Public = chk_Public.isOn;

            // Send off the updated config to the server
            SettingsManager.EmitToServerCTSPacket(new CTSServerConfig() { config = ss });
        }

        private void OnWorldGalleryClicked()
        {
            SysMenuStatic.CloseSysMenus();

            WorldPanelUI.New();
        }

        private void OnContentPermissionsClicked()
        {
            SysMenuStatic.CloseSysMenus();

            ContentFilterUI cui = ContentFilterUI.New();

            cui.spj = Permissions;

            cui.OnFinishConfiguring += () => dirty = true;
        }

        private void OnClearCachesClicked()
        {
            if(Directory.Exists(Utils.WorldCacheRootDir))
                Directory.Delete(Utils.WorldCacheRootDir, true);
        }
    }
}
