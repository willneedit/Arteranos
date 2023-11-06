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
        private ServerOnlineData? ssj = null;

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

        private void OnInfoClicked() => throw new NotImplementedException();

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
            btn_Visit.gameObject.SetActive(true);
            btn_Delete.gameObject.SetActive(false);

            spd = spd ?? SettingsManager.ServerCollection.Get(new Uri(serverURL));
            ssj = ssj ?? ServerGallery.RetrieveServerSettings(serverURL);

            if(ssj != null)
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
            ssj = null;
            spd = null;
            PopulateServerData();
        }

        private void VisualizeServerData()
        {
            if(ssj == null) return;

            Texture2D icon = new(2, 2);
            ImageConversion.LoadImage(icon, ssj.Value.Icon);

            img_Icon.sprite = Sprite.Create(icon,
                new Rect(0, 0, icon.width, icon.height),
                Vector2.zero);

            string serverstr = spd?.Name;

            string CurrentWorld = ssj?.CurrentWorld;
            int CurrentUsers = (ssj != null) ? ssj.Value.UserPublicKeys.Count : -1;

            if(string.IsNullOrEmpty(CurrentWorld)) CurrentWorld = null;

            if(CurrentUsers >= 0) serverstr = $"{spd?.Name} (Users: {CurrentUsers})";

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
            ServerGallery.StoreServerSettings(serverURL, ssj.Value);

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

        public async Task RefreshServerDataAsync()
        {
            (ServerPublicData? spd, ServerOnlineData? sod) = await ServerPublicData.GetServerDataAsync(serverURL, 1);
            this.spd = spd;
            this.ssj = sod;

            if (sod != null) StoreUpdatedServerListItem();
        }
    }
}
