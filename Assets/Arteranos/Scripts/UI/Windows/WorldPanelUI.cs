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
using Arteranos.Web;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using System.Linq;
using System.Collections;

namespace Arteranos.UI
{
    internal struct Collection
    {
        public string worldURL;
        public int serversCount;
        public int usersCount;
        public int friendsMax;
    }

    public class WorldPanelUI : UIBehaviour
    {
        [SerializeField] private GameObject grp_WorldPanelSample;

        [SerializeField] private TMP_Text lbl_PageCount;
        [SerializeField] private Button btn_First;
        [SerializeField] private Button btn_FRev;
        [SerializeField] private Button btn_Previous;
        [SerializeField] private Button btn_Next;
        [SerializeField] private Button btn_FFwd;
        [SerializeField] private Button btn_Last;

        [SerializeField] private TMP_InputField txt_AddWorldURL;
        [SerializeField] private Button btn_AddWorld;

        public string pageCountPattern = null;

        private Client cs = null;

        private readonly List<ServerInfo> serverInfos = new();
        private readonly Dictionary<string, Collection> worldlist = new();
        private readonly List<string> sortedWorldList = new();
        private Mutex DictMutex = null;

        private int currentPage = 0;
        private int maxPage = 0;

        public static WorldPanelUI New()
        {
            GameObject go = Instantiate(Resources.Load<GameObject>("UI/UI_WorldPanel"));
            return go.GetComponent<WorldPanelUI>();
        }

        protected override void Awake()
        {
            base.Awake();

            pageCountPattern = lbl_PageCount.text;

            DictMutex = new();
            btn_AddWorld.onClick.AddListener(OnAddWorldClicked);
        }

        protected override void OnDestroy()
        {
            DictMutex.Close();
            DictMutex = null;
            base.OnDestroy();
        }

        protected override void Start()
        {
            base.Start();

            cs = SettingsManager.Client;

            lbl_PageCount.text = "Loading...";

            _ = CollateServersData();
        }

        private async void AddListEntry(string url, CancellationToken token)
        {
            if (sortedWorldList.Contains(url)) return;

            WorldInfo? wi = await WorldGallery.LoadWorldInfoAsync(url, token);
            WorldMetaData wmd = wi?.metaData;

            // Filter out the worlds which go against to _your_ preferences.
            if (wmd?.ContentRating == null || !wmd.ContentRating.IsInViolation(SettingsManager.Client.ContentFilterPreferences))
                sortedWorldList.Add(url);
        }

        private async Task CollateServersData()
        {
            async Task UpdateOne(ServerInfo serverInfo)
            {
                await serverInfo.Update(1);

                // Server offline or has no world loaded?
                if(!serverInfo.IsOnline || string.IsNullOrEmpty(serverInfo.CurrentWorld)) return;

                int friends = serverInfo.FriendCount;

                DictMutex.WaitOne();

                if(worldlist.TryGetValue(serverInfo.CurrentWorld, out Collection list))
                {
                    list.serversCount++;
                    list.usersCount += serverInfo.UserCount;
                    if(friends > list.friendsMax) list.friendsMax = friends;
                    worldlist[serverInfo.CurrentWorld] = list;
                }
                else
                {
                    worldlist[serverInfo.CurrentWorld] = new()
                    {
                        worldURL = serverInfo.CurrentWorld,
                        friendsMax = friends,
                        serversCount = 1,
                        usersCount = serverInfo.UserCount
                    };
                }

                DictMutex.ReleaseMutex();
            }

            CancellationTokenSource cts = new();

            TaskPool<ServerInfo> pool = new(10);

            foreach (var entry in SettingsManager.ServerCollection.Dump(DateTime.MinValue))
                serverInfos.Add(new(entry.Address, entry.Port));

            foreach (ServerInfo info in serverInfos) pool.Schedule(info, UpdateOne);

            await pool.Run(cts.Token);

            sortedWorldList.Clear();

            if (!string.IsNullOrEmpty(SettingsManager.CurrentWorld))
                AddListEntry(SettingsManager.CurrentWorld, cts.Token);

            foreach (string url in cs.WorldList)
                AddListEntry(url, cts.Token);

            foreach (string url in worldlist.Keys)
                AddListEntry(url, cts.Token);

            SettingsManager.StartCoroutineAsync(() => ShowPage(0));
        }

        private IEnumerator ShowPage(int currentPage)
        {
            yield return null;

            this.currentPage = currentPage;
            maxPage = (sortedWorldList.Count + 5) / 6;

            Transform panels = grp_WorldPanelSample.transform.parent;

            for (int i = 1; i < panels.childCount; i++)
                Destroy(panels.GetChild(i).gameObject);

            int startIndex = currentPage * 6;
            int endIndex = startIndex + 6;
            if(endIndex > sortedWorldList.Count) endIndex = sortedWorldList.Count;

            for(int i = startIndex; i < endIndex; i++)
            {
                GameObject go = Instantiate(grp_WorldPanelSample, panels);
                WorldListItem wli = go.GetComponentInChildren<WorldListItem>();
                wli.WorldURL = sortedWorldList[i];
                go.SetActive(true);
            }

            lbl_PageCount.text = string.Format(pageCountPattern, currentPage + 1, maxPage);
        }

        private void OnAddWorldClicked() => throw new NotImplementedException();
    }
}
