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
using Arteranos.Web;
using static Codice.CM.WorkspaceServer.WorkspaceTreeDataStore;

namespace Arteranos.UI
{
    public class PrefPanel_Moderation : UIBehaviour
    {
        public TMP_InputField txt_WorldURL = null;
        public Button btn_LoadWorld = null;
        public Button btn_WorldGallery = null;
        public Toggle chk_Guests = null;
        public Toggle chk_CustomAvatars = null;
        public Toggle chk_Flying = null;

        public ProgressUI bp_ProgressUI = null;

        private ClientSettings cs = null;
        private bool dirty = false;

        protected override void Awake()
        {
            btn_LoadWorld.onClick.AddListener(OnLoadWorldButtonClicked);
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();

            cs = SettingsManager.Client;

            // Reset the state as it's the initial state, not the blank slate.
            dirty = false;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            // Might be to disabled before it's really started, so cs may be null yet.
            if(dirty) cs?.SaveSettings();
            dirty = false;
        }

        private void OnLoadWorldButtonClicked()
        {
            // Lengthy operation, avoid double clicking.
            btn_LoadWorld.interactable = false;

            string url = txt_WorldURL.text;

            ProgressUI pui = Instantiate(bp_ProgressUI);

            //pui.PatienceThreshold = 0f;
            //pui.AlmostFinishedThreshold = 0f;

            pui.AllowCancel = true;

            (pui.Executor, pui.Context) = WorldDownloader.PrepareDownloadWorld(url, true);

            pui.Completed += OnLoadWorldComplete;
            pui.Faulted += OnLoadWorldFaulted;
        }

        private void OnLoadWorldFaulted(Exception ex, Context _context)
        {
            Debug.LogWarning($"Error in loading world: {ex.Message}");

            btn_LoadWorld.interactable = true;
        }

        private void OnLoadWorldComplete(Context _context)
        {
            string worldABF = WorldDownloader.GetWorldAssetBundle(_context);

            Debug.Log($"Download complete, world={worldABF}");

            // Deploy the scene loader.
            GameObject go = new("_SceneLoader");
            go.AddComponent<Persistence>();
            SceneLoader sl = go.AddComponent<SceneLoader>();
            sl.Name = worldABF;

            // Or, the newly scene will destroy both the progress indicator and the system menu.
            //btn_LoadWorld.interactable = true;

        }
    }
}
