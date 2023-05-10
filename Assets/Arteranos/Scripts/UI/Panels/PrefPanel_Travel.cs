/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine.EventSystems;
using UnityEngine.UI;

using Arteranos.Core;

namespace Arteranos.UI
{
    public class PrefPanel_Travel : UIBehaviour
    {
        public Button btn_WorldGallery = null;
        public Button btn_ServerGallery = null;
        public Button btn_SetContent = null;

        private ClientSettings cs = null;
        private bool dirty = false;

        protected override void Awake()
        {
            base.Awake();

            btn_WorldGallery.onClick.AddListener(OnWorldGalleryClicked);
            btn_ServerGallery.onClick.AddListener(OnServerGalleryClicked);
            btn_SetContent.onClick.AddListener(OnSetContentClicked);
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
            SysMenuKind.CloseSystemMenus();

            ContentFilterUI cui = ContentFilterUI.New();

            cui.spj = SettingsManager.Client.ContentFilterPreferences;

            cui.OnFinishConfiguring +=
                () => SettingsManager.Client?.SaveSettings();
        }
    }
}