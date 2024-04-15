/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Arteranos.Core;
using Arteranos.Web;
using System.Threading.Tasks;
using System.Collections;
using System;

namespace Arteranos.UI
{
    public class ServerListItem : ListItemBase
    {
        public string PeerID = null;
        public RawImage img_Icon = null;
        public TMP_Text lbl_Caption = null;

        public Color BgndRegular;
        public Color BgndWarning;

        private HoverButton btn_Add = null;
        private HoverButton btn_Info = null;
        private HoverButton btn_Visit = null;
        private HoverButton btn_Delete = null;

        private ServerInfo si = null;

        public static ServerListItem New(Transform parent, string PeerID)
        {
            GameObject go = Instantiate(BP.I.UIComponents.ServerListItem);
            go.transform.SetParent(parent, false);
            ServerListItem serverListItem = go.GetComponent<ServerListItem>();
            serverListItem.PeerID = PeerID;
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

            si = new(PeerID);

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

            btn_Background.image.color = (si.UsesCustomTOS && !SettingsManager.Client.AllowCustomTOS)
                ? BgndWarning
                : BgndRegular;

            if(si.IsValid)
            {
                StartCoroutine(VisualizeServerData());

                btn_Add.gameObject.SetActive(false);
                btn_Delete.gameObject.SetActive(true);
                return;
            }


            lbl_Caption.text = $"({PeerID}) (Unknown)";

            btn_Add.gameObject.SetActive(true);
            btn_Delete.gameObject.SetActive(true);
        }

        public Task UpdateServerData()
        {
            IEnumerator PSDCoroutine()
            {
                si = new(PeerID);

                yield return null;

                PopulateServerData();
            }

            SettingsManager.StartCoroutineAsync(PSDCoroutine);

            return Task.CompletedTask;
        }

        private IEnumerator VisualizeServerData()
        {
            yield return Utils.DownloadIconCoroutine(si.ServerIcon, _tex => img_Icon.texture = _tex);

            if(!si.IsOnline)
            {
                lbl_Caption.text = $"{si.Name} (Offline)";
                yield break;
            }

            lbl_Caption.text = 
                $"Server: {$"{si.Name}"} (Users: {si.UserCount}, Friends: {si.FriendCount})\n" +
                $"Current World: {si.CurrentWorldName}";
        }

        private void OnVisitClicked()
        {
            btn_Visit.interactable = false;                
            // NOTE: Initiating transition, needs to be unhooked from the server list item, which will vanish!
            SettingsManager.StartCoroutineAsync(() => ConnectionManager.ConnectToServer(PeerID, null));

            // Can be removed because of the TOS afreement window, ot other things.
            if(btn_Visit != null) btn_Visit.interactable = true;
        }

        private async void OnAddClicked()
        {
            Client cs = SettingsManager.Client;

            // Put it down into our bookmark list.
            if(!cs.ServerList.Contains(PeerID))
            {
                cs.ServerList.Add(PeerID);
                cs.Save();
            }

            await UpdateServerData();
        }

        private void OnDeleteClicked()
        {
            Client cs = SettingsManager.Client;

            // Strike it from our list
            if(cs.ServerList.Contains(PeerID))
                cs.ServerList.Remove(PeerID);

            // The server public is entered with the server port, not the MD port.
            string key = si.SPKDBKey;

            if(cs.ServerPasses.ContainsKey(key))
                cs.ServerPasses.Remove(key);

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
