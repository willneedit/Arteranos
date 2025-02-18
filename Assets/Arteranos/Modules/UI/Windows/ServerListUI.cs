/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;
using UnityEngine.UI;

using Arteranos.Core;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;

namespace Arteranos.UI
{
    public class ServerListUI : ActionPage
    {

        [SerializeField] private RectTransform lvc_ServerList;
        [SerializeField] private Button btn_Reload;
        [SerializeField] private GameObject grp_ServerList;
        [SerializeField] private GameObject grp_NoServersNotice;

        private Client cs = null;

        private readonly Dictionary<string, ServerListItem> ServerList = new();

        protected override void Awake()
        {
            base.Awake();

            btn_Reload.onClick.AddListener(OnReloadClicked);

            grp_ServerList.SetActive(false);
            btn_Reload.interactable = false;
            grp_NoServersNotice.SetActive(true);
        }

        protected override void Start()
        {
            base.Start();

            IEnumerator PopulateCoroutine()
            {
                yield return null;
                bool hasServers = false;

                cs = G.Client;

                // Put these servers in this list in front
                foreach (string PeerIDString in cs.ServerList)
                {
                    ServerList[PeerIDString] = ServerListItem.New(lvc_ServerList.transform, PeerIDString);
                    hasServers = true;
                }

                foreach (ServerInfo si in ServerInfo.Dump())
                {
                    string PeerIDString = si.PeerID.ToString();
                    if (!ServerList.ContainsKey(PeerIDString))
                    {
                        ServerList[PeerIDString] = ServerListItem.New(lvc_ServerList.transform, PeerIDString);
                        hasServers = true;
                    }

                    yield return null;
                }

                grp_ServerList.SetActive(hasServers);
                btn_Reload.interactable = hasServers;
                grp_NoServersNotice.SetActive(!hasServers);
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
