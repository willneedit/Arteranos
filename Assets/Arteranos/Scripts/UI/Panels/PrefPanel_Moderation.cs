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
using System;

namespace Arteranos.UI
{
    public class PrefPanel_Moderation : UIBehaviour
    {
        public Button btn_WorldGallery = null;
        public Toggle chk_Guests = null;
        public Toggle chk_CustomAvatars = null;
        public Toggle chk_Flying = null;

        private ClientSettings cs = null;
        private ServerSettings ss = null;

        private bool dirty = false;

        protected override void Awake()
        {
            base.Awake();

            btn_WorldGallery.onClick.AddListener(OnWorldGalleryClicked);
        }

        protected override void Start()
        {
            base.Start();

            cs = SettingsManager.Client;
            ss = SettingsManager.Server;

            chk_Guests.isOn = ss.AllowGuests;
            chk_CustomAvatars.isOn = ss.AllowCustomAvatars;
            chk_Flying.isOn = ss.AllowFlying;

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

                ss.AllowFlying = chk_Flying.isOn;
                ss.AllowCustomAvatars = chk_CustomAvatars.isOn;
                ss.AllowGuests = chk_Guests.isOn;
            }

            // Might be to disabled before it's really started, so cs may be null yet.
            if(dirty) cs?.SaveSettings();
            dirty = false;
        }

        private void OnWorldGalleryClicked()
        {
            SysMenuKind.CloseSystemMenus();

            WorldListUI.New();
        }
    }
}
