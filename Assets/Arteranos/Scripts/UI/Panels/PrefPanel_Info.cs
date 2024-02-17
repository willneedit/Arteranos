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
using Arteranos.Core;
using Ipfs;

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
        public TMP_Text lbl_LauncherLink = null;

        public Button btn_CopyToClipboard = null;
        public Button btn_license = null;
        public Button btn_3rdParty = null;

        protected override void Awake()
        {
            base.Awake();

            Version v = Version.Load();

            lbl_Version.text = v.Full;
            lbl_DeviceName.text = SystemInfo.deviceName;
            lbl_DeviceType.text = SystemInfo.deviceType.ToString();
            lbl_GraphicsDevice.text = SystemInfo.graphicsDeviceName;
            lbl_GraphicsVersion.text = SystemInfo.graphicsDeviceVersion;


            btn_license.onClick.AddListener(() => OnLicenseClicked(false));
            btn_3rdParty.onClick.AddListener(() => OnLicenseClicked(true));
            btn_CopyToClipboard.onClick.AddListener(OnClipboardClicked);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            MultiHash RemotePeerId = SettingsManager.GetServerConnectionData();
            string lltext = "Offline";

            if (RemotePeerId != null)
                lltext = $"arteranos://{RemotePeerId}/";

            lbl_LauncherLink.text = lltext;
        }

        private void Update()
        {
            btn_CopyToClipboard.interactable = NetworkStatus.GetOnlineLevel() != OnlineLevel.Offline;
        }

        private void OnLicenseClicked(bool thirdparty)
        {
            SysMenu.CloseSysMenus();

            LicenseTextUI.New(thirdparty);
        }

        private void OnClipboardClicked() 
            => GUIUtility.systemCopyBuffer = lbl_LauncherLink.text;

    }
}
