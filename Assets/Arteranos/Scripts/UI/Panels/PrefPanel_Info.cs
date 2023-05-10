/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Arteranos.Core;
using Arteranos.Services;

namespace Arteranos.UI
{
    public class PrefPanel_Info : UIBehaviour
    {
        public TMP_Text lbl_Version = null;
        public TMP_Text lbl_DeviceName = null;
        public TMP_Text lbl_DeviceType = null;
        public TMP_Text lbl_GraphicsDevice = null;
        public TMP_Text lbl_GraphicsVersion = null;
        public TMP_Text lbl_ExternalIPAddr = null;
        public Button btn_3rdParty = null;

        private ClientSettings cs = null;

        protected override void Awake()
        {
            base.Awake();

            lbl_Version.text = Application.version;
            lbl_DeviceName.text = SystemInfo.deviceName;
            lbl_DeviceType.text = SystemInfo.deviceType.ToString();
            lbl_GraphicsDevice.text = SystemInfo.graphicsDeviceName;
            lbl_GraphicsVersion.text = SystemInfo.graphicsDeviceVersion;
            
            NetworkStatus networkStatus = FindObjectOfType<NetworkStatus>();
            System.Net.IPAddress extip = networkStatus.ExternalAddress;

            lbl_ExternalIPAddr.text = (extip != null) ? extip.ToString() : "Unknown";

            btn_3rdParty.onClick.AddListener(On3rdPartyClicked);
        }

        private void On3rdPartyClicked()
        {
            SysMenuKind.CloseSystemMenus();

            LicenseTextUI.New();
        }

        protected override void Start()
        {
            base.Start();

            cs = SettingsManager.Client;

        }

    }
}
