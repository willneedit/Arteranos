/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Arteranos.Core;
using Arteranos.Web;
using System.Threading.Tasks;
using System.Collections;

namespace Arteranos.UI
{
    public class ServerListItem : ListItemBase
    {
        public string serverURL = null;
        public Image img_Icon = null;
        public TMP_Text lbl_Caption = null;

        private HoverButton btn_Add = null;
        private HoverButton btn_Info = null;
        private HoverButton btn_Visit = null;
        private HoverButton btn_Delete = null;


        private ServerPublicData? spd = null;
        private ServerDescription? sod = null;
        private bool? IsOnline = null;

        public static ServerListItem New(Transform parent, string url)
        {
            GameObject go = Instantiate(Resources.Load<GameObject>("UI/Components/ServerListItem"));
            go.transform.SetParent(parent, false);
            ServerListItem serverListItem = go.GetComponent<ServerListItem>();
            serverListItem.serverURL = url;
            return serverListItem;
        }

        protected override void Awake()
        {
            base.Awake();

            btn_Add = btns_ItemButton[0];
            btn_Info = btns_ItemButton[1];
            btn_Visit = btns_ItemButton[2];
            btn_Delete = btns_ItemButton[3];

            btn_Add.onClick.AddListener(OnAddClicked);
            btn_Info.onClick.AddListener(OnInfoClicked);
            btn_Visit.onClick.AddListener(OnVisitClicked);
            btn_Delete.onClick.AddListener(OnDeleteClicked);
        }

        protected override void Start()
        {
            base.Start();

            PopulateServerData();
        }

        public void PopulateServerData()
        {
            // Could be that the list item bas been deleted in th meantime.
            // Or, the entire list.
            if(btn_Add == null) return;

            btn_Add.gameObject.SetActive(false);
            btn_Visit.gameObject.SetActive(IsOnline ?? true);
            btn_Delete.gameObject.SetActive(false);

            spd ??= SettingsManager.ServerCollection.Get(new Uri(serverURL));
            sod ??= ServerGallery.RetrieveServerSettings(serverURL);

            if(sod != null)
            {
                VisualizeServerData();
                btn_Add.gameObject.SetActive(false);
                btn_Delete.gameObject.SetActive(true);
                return;
            }


            // No data for that server's URL, maybe the manually entered URL is not added yet.
            lbl_Caption.text = $"({serverURL})";

            // Either try to add an unconfirmed URL, or to undo that attempt, or
            // try to retrieve the probably offline server.
            btn_Add.gameObject.SetActive(true);
            btn_Delete.gameObject.SetActive(true);
        }

        public void InvalidateServerData()
        {
            sod = null;
            spd = null;
            PopulateServerData();
        }

        private void VisualizeServerData()
        {
            if (!(IsOnline ?? false))
            {
                string offlstr = (IsOnline == null) ? "Unknown" : "Offline";
                lbl_Caption.text = $"{spd?.Key()} ({offlstr})";

                // TODO Replace with a "Unknown state" and a "Server offline" icon.
                if(sod != null) Utils.ShowImage(sod.Value.Icon, img_Icon);
                return;
            }

            if (sod == null) return;

            Utils.ShowImage(sod.Value.Icon, img_Icon);

            string CurrentWorld = string.Empty; // UNDONE sod?.CurrentWorld;
            int CurrentUsers = 0; // UNDONE sod.Value.UserPublicKeys.Count;

            if (string.IsNullOrEmpty(CurrentWorld)) CurrentWorld = null;

            string serverstr = $"{spd?.Key()} (Users: {CurrentUsers})";

            lbl_Caption.text = $"Server: {serverstr}\nCurrent World: {CurrentWorld ?? "Unknown"}";
        }

        private async void OnVisitClicked()
        {
            btn_Visit.interactable = false;

            if(!string.IsNullOrEmpty(serverURL))
                await ConnectionManager.ConnectToServer(serverURL);

            btn_Visit.interactable = true;
        }

        private void StoreUpdatedServerListItem()
        {
            // Transfer the metadata in our persistent storage.
            ServerGallery.StoreServerSettings(serverURL, sod.Value);

            Client cs = SettingsManager.Client;

            // Put it down into our bookmark list.
            if(!cs.ServerList.Contains(serverURL))
            {
                cs.ServerList.Add(serverURL);
                cs.Save();
            }

            // Visualize the changed state.
            PopulateServerData();
        }

        private async void OnAddClicked()
        {
            Client cs = SettingsManager.Client;

            // Put it down into our bookmark list.
            if(!cs.ServerList.Contains(serverURL))
            {
                cs.ServerList.Add(serverURL);
                cs.Save();
            }

            await RefreshServerDataAsync();
        }

        private void OnDeleteClicked()
        {
            Client cs = SettingsManager.Client;

            // Remove the metadata from the persistent storage.
            ServerGallery.DeleteServerSettings(serverURL);

            // Then, strike it from our list
            if(cs.ServerList.Contains(serverURL))
            {
                cs.ServerList.Remove(serverURL);
                cs.Save();
            }

            // And, zip, gone.
            Destroy(gameObject);
        }

        private async void OnInfoClicked()
        {
            btn_Info.interactable = false;
            await RefreshServerDataAsync(1, false);
            btn_Info.interactable = true;

            ServerInfoUI.New(spd, sod);
        }

        public async Task RefreshServerDataAsync(int timeout = 1, bool saveOnlineData = true)
        {
            (ServerPublicData? spd, ServerDescription? sod) = await ServerPublicData.GetServerDataAsync(serverURL, timeout);
            this.spd = spd;

            // ServerOnlineData could be saved, retain them.
            if (sod == null)
                IsOnline = false;
            else
            {
                IsOnline = true;
                this.sod = sod;
            }

            if (sod != null && saveOnlineData) StoreUpdatedServerListItem();

            IEnumerator UpdateServerStateCoroutine()
            {
                yield return null;
                VisualizeServerData();

                btn_Visit.gameObject.SetActive(IsOnline ?? true);
            };

            SettingsManager.StartCoroutineAsync(UpdateServerStateCoroutine);
        }
    }
}
