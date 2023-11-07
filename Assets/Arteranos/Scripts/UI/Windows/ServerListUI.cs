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

using Arteranos.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Arteranos.UI
{
    public class ServerListUI : UIBehaviour
    {

        public RectTransform lvc_ServerList;
        public TMP_InputField txt_AddServerURL;
        public Button btn_AddServer;
        public Button btn_Reload;

        private Client cs = null;

        private readonly Dictionary<string, ServerListItem> ServerList = new();

        public static ServerListUI New()
        {
            GameObject go = Instantiate(Resources.Load<GameObject>("UI/UI_ServerList"));
            return go.GetComponent<ServerListUI>();
        }

        protected override void Awake()
        {
            base.Awake();

            btn_AddServer.onClick.AddListener(OnAddWorldClicked);
            btn_Reload.onClick.AddListener(OnReloadClicked);
        }

        protected override void Start()
        {
            base.Start();

            cs = SettingsManager.Client;

            foreach(string url in cs.ServerList)
                ServerList[url] = ServerListItem.New(lvc_ServerList.transform, url);
        }

        private void OnAddWorldClicked()
        {
            string url = txt_AddServerURL.text;
            ServerList[url] = ServerListItem.New(lvc_ServerList.transform, url);
        }

        private async void OnReloadClicked()
        {
            static Task DoUpdate(ServerListItem server) 
                => server.RefreshServerDataAsync();

            btn_Reload.interactable = false;

            TaskPool<ServerListItem> pool = new();

            Debug.Log($"Reload started, queued {ServerList.Count} servers");

            foreach (ServerListItem server in ServerList.Values)
            {
                server.InvalidateServerData();
                pool.Schedule(server, DoUpdate);
            }

            await pool.Run();

            Debug.Log("Reload finished.");
            if (btn_Reload != null) btn_Reload.interactable = true;
        }
    }
}
