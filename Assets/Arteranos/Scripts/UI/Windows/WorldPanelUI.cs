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
using System.Collections;
using System.Linq;

namespace Arteranos.UI
{
    internal struct Collection
    {
        public string worldURL;
        public int serversCount;
        public int usersCount;
        public int friendsMax;
        public WorldInfo? worldInfo;
        public bool favourited;
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

        private string pageCountPattern = null;

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

            btn_First.onClick.AddListener(() => SwitchToPage(0, -1));
            btn_FRev.onClick.AddListener(() => SwitchToPage(-10, 0));
            btn_Previous.onClick.AddListener(() => SwitchToPage(-1, 0));
            btn_Next.onClick.AddListener(() => SwitchToPage(1, 0));
            btn_FFwd.onClick.AddListener(() => SwitchToPage(10, 0));
            btn_Last.onClick.AddListener(() => SwitchToPage(0, 1));
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

        private async Task AddListEntry(string url, CancellationToken token, bool front = false)
        {
            if (sortedWorldList.Contains(url)) return;

            if (worldlist.TryGetValue(url, out Collection list))
            {
                if(list.worldInfo == null)
                {
                    WorldInfo? wi = await WorldGallery.LoadWorldInfoAsync(url, token);
                    list.worldInfo = wi;
                    worldlist[url] = list;
                }
            }
            else // Manually edited?
            {
                WorldInfo? wi = await WorldGallery.LoadWorldInfoAsync(url, token);
                worldlist[url] = new()
                {
                    worldURL = url,
                    friendsMax = 0,
                    serversCount = 0,
                    usersCount = 0,
                    worldInfo = wi,
                    favourited = false
                };
            }

            WorldMetaData wmd = list.worldInfo?.metaData;

            // Filter out the worlds which go against to _your_ preferences.
            if (wmd?.ContentRating == null || !wmd.ContentRating.IsInViolation(SettingsManager.Client.ContentFilterPreferences))
            {
                if(!front) sortedWorldList.Add(url);
                else sortedWorldList.Insert(0, url);
            }
                
        }

        private int ScoreWorld(string url) 
        {
            if(!worldlist.TryGetValue(url, out Collection list)) return -10000;

            int score = 0;

            score += list.serversCount; // Servers get one point.

            score += list.usersCount * 5; // Users get five points.

            score += list.friendsMax * 20; // Friends get twenty points.

            score += list.favourited ? 100000 : 0; // A class for its own.

            return score;
        }

        private async Task CollateServersData()
        {
            async Task UpdateOne(ServerInfo serverInfo)
            {
                await serverInfo.Update();

                // Server offline or has no world loaded?
                string url = serverInfo.CurrentWorld;
                if (!serverInfo.IsOnline || string.IsNullOrEmpty(url)) return;

                int friends = serverInfo.FriendCount;

                DictMutex.WaitOne();

                if(worldlist.TryGetValue(url, out Collection list))
                {
                    list.serversCount++;
                    list.usersCount += serverInfo.UserCount;
                    if(friends > list.friendsMax) list.friendsMax = friends;
                    worldlist[url] = list;
                }
                else
                {
                    worldlist[url] = new()
                    {
                        worldURL = url,
                        friendsMax = friends,
                        serversCount = 1,
                        usersCount = serverInfo.UserCount
                    };
                }

                DictMutex.ReleaseMutex();
            }

            CancellationTokenSource cts = new();

            TaskPool<ServerInfo> pool = new(20);

            foreach (var entry in SettingsManager.ServerCollection.Dump(DateTime.MinValue))
                serverInfos.Add(new(entry.Address, entry.MDPort));

            foreach (ServerInfo info in serverInfos) pool.Schedule(info, UpdateOne);

            await pool.Run(cts.Token);

            sortedWorldList.Clear();

            if (!string.IsNullOrEmpty(SettingsManager.CurrentWorld))
                await AddListEntry(SettingsManager.CurrentWorld, cts.Token);

            foreach (string url in cs.WorldList)
            {
                await AddListEntry(url, cts.Token);
                Collection list = worldlist[url];
                list.favourited = true;
                worldlist[url] = list;
            }

            string[] keys = worldlist.Keys.ToArray();
            foreach (string url in keys)
                await AddListEntry(url, cts.Token);

            sortedWorldList.Sort((x, y) => ScoreWorld(y) - ScoreWorld(x));

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
            int endIndex = startIndex + 5;
            if(endIndex > sortedWorldList.Count) endIndex = sortedWorldList.Count;

            for(int i = startIndex; i < endIndex; i++)
            {
                GameObject go = Instantiate(grp_WorldPanelSample, panels);
                WorldListItem wli = go.GetComponentInChildren<WorldListItem>();
                wli.WorldURL = sortedWorldList[i];
                if (worldlist.TryGetValue(wli.WorldURL, out Collection list))
                {
                    wli.WorldName = list.worldInfo?.metaData.WorldName;
                    wli.ScreenshotPNG = list.worldInfo?.screenshotPNG;
                    wli.LastAccessed = list.worldInfo?.updated ?? DateTime.MinValue;
                    wli.ServersCount = list.serversCount;
                    wli.UsersCount = list.usersCount;
                    wli.FriendsMax = list.friendsMax;

                    WorldMetaData wmd = list.worldInfo?.metaData;
                    wli.AllowedForThis = !(wmd?.ContentRating != null && wmd.ContentRating.IsInViolation(SettingsManager.ActiveServerData.Permissions));
                }
                go.SetActive(true);
            }

            lbl_PageCount.text = string.Format(pageCountPattern, currentPage + 1, maxPage);
        }

        private void SwitchToPage(int difference, int location)
        {
            int newPage = location switch
            {
                < 0 => difference,
                  0 => currentPage + difference,
                > 0 => maxPage - 1 - difference
            };

            if(newPage >= maxPage) newPage = maxPage - 1;
            else if(newPage < 0) newPage = 0;

            StartCoroutine(ShowPage(newPage));
        }
        
        private async void OnAddWorldClicked()
        {
            CancellationTokenSource cts = new();

            await AddListEntry(txt_AddWorldURL.text, cts.Token, true);

            SettingsManager.StartCoroutineAsync(() => ShowPage(0));
        }
    }
}
