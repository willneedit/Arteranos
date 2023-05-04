/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Arteranos.Core;
using Arteranos.Web;
using System.Threading.Tasks;

namespace Arteranos.UI
{
    public class PrefPanel_Travel : UIBehaviour
    {
        public Button btn_WorldGallery = null;
        public Button btn_ServerGallery = null;
        public Button btn_SetContent = null;
        public Button btn_ToggleHost = null;
        public TextMeshProUGUI btn_ToggleHostCaption = null;

        private ClientSettings cs = null;
        private bool dirty = false;

        protected override void Awake()
        {
            base.Awake();

            btn_WorldGallery.onClick.AddListener(OnWorldGalleryClicked);
            btn_ServerGallery.onClick.AddListener(OnServerGalleryClicked);
            btn_SetContent.onClick.AddListener(OnSetContentClicked);
            btn_ToggleHost.onClick.AddListener(OnToggleHostClicked);
        }

        protected override void Start()
        {
            base.Start();

            cs = SettingsManager.Client;

            // Reset the state as it's the initial state, not the blank slate.
            dirty = false;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            UpdateConnectionState();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            // Might be to disabled before it's really started, so cs may be null yet.
            if(dirty) cs?.SaveSettings();
            dirty = false;
        }

        private void OnWorldGalleryClicked()
        {
            SysMenuKind.CloseSystemMenus();

            WorldListUI.New();
        }

        private void OnServerGalleryClicked()
        {
            SysMenuKind.CloseSystemMenus();

            ServerListUI.New();
        }

        private void OnSetContentClicked()
        {
            throw new NotImplementedException();
        }

        private async void OnToggleHostClicked()
        {
            btn_ToggleHost.interactable = false;

            if(ConnectionManager.CanDoConnect())
                ConnectionManager.StartHost();
            else
                ConnectionManager.StopHost();

            // Caution, things could change in the meantime.
            await Task.Delay(3000);

            if(btn_ToggleHost != null)
            {
                // MissingReferenceException happens if one closes the menu before
                // we finished, or the initial world transition on entering the
                // local server.
                UpdateConnectionState();

                btn_ToggleHost.interactable = true;
            }
        }

        private void UpdateConnectionState()
        {
            if(ConnectionManager.CanDoConnect())
                btn_ToggleHostCaption.text = "Enter local server";
            else if(ConnectionManager.CanGetConnected())
                btn_ToggleHostCaption.text = "Stop local server";
            else
                btn_ToggleHostCaption.text = "Go offline";
        }

    }
}
