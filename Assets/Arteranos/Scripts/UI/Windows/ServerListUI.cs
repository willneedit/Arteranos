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
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace Arteranos.UI
{
    public class ServerListUI : UIBehaviour
    {

        public RectTransform lvc_ServerList;
        public TMP_InputField txt_AddServerURL;
        public Button btn_AddServer;
        public Button btn_Reload;

        private ClientSettings cs = null;

        private readonly Dictionary<string, ServerListItem> ServerList = new();
        private readonly Queue<string> RemainingServerList = new();
        private int InProgress = 0;

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
            btn_Reload.interactable = false;

            void OnUpdateFinished(ServerListItem obj)
            {
                obj.OnUpdateFinished -= OnUpdateFinished;
                Interlocked.Decrement(ref InProgress);
            }

            foreach(string server in ServerList.Keys)
            {
                RemainingServerList.Enqueue(server);
                ServerList[server].InvalidateServerData();
            }

            Debug.Log($"Reload started, queued {RemainingServerList.Count} servers");

            while(InProgress > 0 || RemainingServerList.Count > 0)
            {
                while(InProgress < 5)
                {
                    if(RemainingServerList.Count == 0) break;

                    string server = RemainingServerList.Dequeue();
                    ServerList[server].OnUpdateFinished += OnUpdateFinished;
                    ServerList[server].ReloadServerData();
                    Interlocked.Increment(ref InProgress);
                }

                await Task.Yield();
            }

            Debug.Log("Reload finished.");

            btn_Reload.interactable = true;
        }
    }
}
