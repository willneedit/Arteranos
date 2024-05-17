/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Arteranos.Core;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
using System;

namespace Arteranos.UI
{
    public class ServerListUI : UIBehaviour
    {

        public RectTransform lvc_ServerList;
        public Button btn_Reload;

        private Client cs = null;

        private readonly Dictionary<string, ServerListItem> ServerList = new();

        public static ServerListUI New()
        {
            GameObject go = Instantiate(BP.I.UI.ServerList);
            return go.GetComponent<ServerListUI>();
        }

        protected override void Awake()
        {
            base.Awake();

            btn_Reload.onClick.AddListener(OnReloadClicked);
        }

        protected override void Start()
        {
            base.Start();

            IEnumerator PopulateCoroutine()
            {
                yield return null;

                cs = SettingsManager.Client;

                // Put these servers in this list in front
                foreach (string PeerIDString in cs.ServerList)
                    ServerList[PeerIDString] = ServerListItem.New(lvc_ServerList.transform, PeerIDString);

                foreach (ServerInfo si in ServerInfo.Dump(DateTime.MinValue))
                {
                    string PeerIDString = si.PeerID.ToString();
                    if (!ServerList.ContainsKey(PeerIDString))
                        ServerList[PeerIDString] = ServerListItem.New(lvc_ServerList.transform, PeerIDString);

                    yield return null;
                }
            }

            StartCoroutine(PopulateCoroutine());
        }

        private void OnReloadClicked()
        {
            static Task DoUpdate(ServerListItem server)
            {
                server.UpdateServerData();
                return Task.CompletedTask;
            }

            IEnumerator ReloadCoroutine()
            {
                btn_Reload.interactable = false;

                TaskPool<ServerListItem> pool = new();

                Debug.Log($"Reload started, queued {ServerList.Count} servers");

                foreach (ServerListItem server in ServerList.Values)
                    pool.Schedule(server, DoUpdate);

                Task t = pool.Run();

                while (!t.IsCompleted) yield return new WaitForEndOfFrame();

                Debug.Log("Reload finished.");

                btn_Reload.interactable = true;
            }

            StartCoroutine(ReloadCoroutine());
        }
    }
}
