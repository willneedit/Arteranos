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

        private ServerInfo si = null;

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

            lbl_Caption.text = "Loading...";
        }

        protected override void Start()
        {
            base.Start();

            si = new(serverURL);

            PopulateServerData();
        }

        public void PopulateServerData()
        {
            // Could be that the list item bas been deleted in th meantime.
            // Or, the entire list.
            if(btn_Add == null) return;

            btn_Add.gameObject.SetActive(false);
            btn_Visit.gameObject.SetActive(si.IsOnline);
            btn_Delete.gameObject.SetActive(false);


            if(si.IsValid)
            {
                VisualizeServerData();
                btn_Add.gameObject.SetActive(false);
                btn_Delete.gameObject.SetActive(true);
                return;
            }


            // No data for that server's URL, maybe the manually entered URL is not added yet.
            lbl_Caption.text = $"({serverURL}) (Unknown)";

            // Either try to add an unconfirmed URL, or to undo that attempt, or
            // try to retrieve the probably offline server.
            btn_Add.gameObject.SetActive(true);
            btn_Delete.gameObject.SetActive(true);
        }

        public async Task UpdateServerData()
        {
            IEnumerator PSDCoroutine()
            {
                yield return null;
                PopulateServerData();
            }
            await si.Update();
            SettingsManager.StartCoroutineAsync(PSDCoroutine);
        }

        private void VisualizeServerData()
        {
            Utils.ShowImage(si.Icon, img_Icon);
            if(!si.IsOnline)
            {
                lbl_Caption.text = $"{si.Name} (Offline)";
                return;
            }

            lbl_Caption.text = 
                $"Server: {$"{si.Name}"} (Users: {si.UserCount}, Friends: {si.FriendCount})\n" +
                $"Current World: {si.CurrentWorldName}";
        }

        private async void OnVisitClicked()
        {
            btn_Visit.interactable = false;                
            await ConnectionManager.ConnectToServer(serverURL);
            btn_Visit.interactable = true;
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

            await UpdateServerData();
        }

        private void OnDeleteClicked()
        {
            Client cs = SettingsManager.Client;

            // Strike it from our list
            if(cs.ServerList.Contains(serverURL))
                cs.ServerList.Remove(serverURL);

            // The server public is entered with the server port, not the MD port.
            string key = si.SPKDBKey;
            if(cs.ServerKeys.ContainsKey(key))
                cs.ServerKeys.Remove(key);

            cs.Save();

            // Delete the online and public data
            si.Delete();

            // And, zip, gone.
            Destroy(gameObject);
        }

        private async void OnInfoClicked()
        {
            btn_Info.interactable = false;
            await UpdateServerData();
            btn_Info.interactable = true;

            ServerInfoUI.New(si);
        }
    }
}
