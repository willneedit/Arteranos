/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
        public Button btn_license = null;
        public Button btn_3rdParty = null;

        protected override void Awake()
        {
            base.Awake();

            lbl_Version.text = Application.version;
            lbl_DeviceName.text = SystemInfo.deviceName;
            lbl_DeviceType.text = SystemInfo.deviceType.ToString();
            lbl_GraphicsDevice.text = SystemInfo.graphicsDeviceName;
            lbl_GraphicsVersion.text = SystemInfo.graphicsDeviceVersion;

            System.Net.IPAddress extip = NetworkStatus.ExternalAddress;

            lbl_ExternalIPAddr.text = (extip != null) ? extip.ToString() : "Unknown";

            btn_license.onClick.AddListener(() => OnLicenseClicked(false));
            btn_3rdParty.onClick.AddListener(() => OnLicenseClicked(true));
        }

        private void OnLicenseClicked(bool thirdparty)
        {
            SysMenuKind.CloseSystemMenus();

            LicenseTextUI.New(thirdparty);
        }
    }
}
