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
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections;
using System.Linq;
using Ipfs;
using Arteranos.Core.Operations;

namespace Arteranos.UI
{
    internal class Collection
    {
        public Cid worldCid;
        public int serversCount;
        public int usersCount;
        public int friendsMax;
        public WorldInfo worldInfo;
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
        private readonly Dictionary<Cid, Collection> worldlist = new();
        private readonly List<Cid> sortedWorldList = new();
        private Mutex DictMutex = null;

        private int currentPage = 0;
        private int maxPage = 0;

        public static WorldPanelUI New()
        {
            GameObject go = Instantiate(BP.I.UI.WorldPanel);
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

        private void AddListEntry(Cid cid, bool front = false)
        {
            if (sortedWorldList.Contains(cid)) return;

            if (worldlist.TryGetValue(cid, out Collection list))
            {
                if(list.worldInfo == null)
                    list.worldInfo = WorldInfo.DBLookup(cid);
            }
            else // Manually added?
            {
                worldlist[cid] = new()
                {
                    worldCid = cid,
                    friendsMax = 0,
                    serversCount = 0,
                    usersCount = 0,
                    worldInfo = WorldInfo.DBLookup(cid),
                    favourited = false
                };
            }

            WorldInfo wmd = list?.worldInfo;

            // Filter out the worlds which go against to _your_ preferences.
            if (wmd?.ContentRating == null || !wmd.ContentRating.IsInViolation(SettingsManager.Client.ContentFilterPreferences))
            {
                if(!front) sortedWorldList.Add(cid);
                else sortedWorldList.Insert(0, cid);
            }
                
        }

        private int ScoreWorld(Cid cid) 
        {
            if(!worldlist.TryGetValue(cid, out Collection list)) return -10000;

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
                Cid Cid = serverInfo.CurrentWorldCid;
                if (!serverInfo.IsOnline || Cid == null) return;

                int friends = serverInfo.FriendCount;

                DictMutex.WaitOne();

                if(worldlist.TryGetValue(Cid, out Collection list))
                {
                    list.serversCount++;
                    list.usersCount += serverInfo.UserCount;
                    if(friends > list.friendsMax) list.friendsMax = friends;
                    worldlist[Cid] = list;
                }
                else
                {
                    worldlist[Cid] = new()
                    {
                        worldCid = Cid,
                        friendsMax = friends,
                        serversCount = 1,
                        usersCount = serverInfo.UserCount
                    };
                }

                DictMutex.ReleaseMutex();
            }

            CancellationTokenSource cts = new();

            TaskPool<ServerInfo> pool = new(20);

            foreach (ServerInfo entry in ServerInfo.Dump(DateTime.MinValue))
                serverInfos.Add(entry);

            foreach (ServerInfo info in serverInfos) pool.Schedule(info, UpdateOne);

            await pool.Run(cts.Token);

            sortedWorldList.Clear();

            if (SettingsManager.WorldCid != null)
            {
                // FIXME Use Coroutine to make this as a nonblocking retrieval?
                WorldInfo wi = await WorldInfo.RetrieveAsync(SettingsManager.WorldCid);
                AddListEntry(wi.WorldCid);
            }

            foreach(WorldInfo wi in WorldInfo.DBList())
            {
                if(!wi.IsFavourited()) continue;

                Cid cid = wi.WorldCid;
                if (cid == null) continue;

                AddListEntry(cid);
                Collection list = worldlist[cid];
                list.favourited = true;
                worldlist[cid] = list;
            }

            Cid[] keys = worldlist.Keys.ToArray();
            foreach (Cid cid in keys)
                AddListEntry(cid);

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
                WorldPaneltem wli = go.GetComponentInChildren<WorldPaneltem>();
                wli.WorldCid = sortedWorldList[i];
                if (worldlist.TryGetValue(wli.WorldCid, out Collection list))
                {
                    wli.WorldInfo = list.worldInfo;
                    wli.ServersCount = list.serversCount;
                    wli.UsersCount = list.usersCount;
                    wli.FriendsMax = list.friendsMax;

                    WorldInfo wmd = list.worldInfo;
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

        private void OnAddWorldClicked()
        {
            btn_AddWorld.interactable = false;

            CancellationTokenSource cts = new();


            IProgressUI pui = ProgressUIFactory.New();

            pui.SetupAsyncOperations(() => AssetUploader.PrepareUploadToIPFS(txt_AddWorldURL.text, true, pin: true)); // World Zip file

            pui.Completed += context =>
            {
                Cid cid = AssetUploader.GetUploadedCid(context);
                ParseWorld(cid);
            };

            pui.Faulted += (ex, context) =>
            {
                btn_AddWorld.interactable = true;
                SettingsManager.StartCoroutineAsync(() => ShowPage(0));
            };
        }

        private void ParseWorld(Cid cid)
        {

            IEnumerator ParseWorldCoroutine()
            {
                yield return null;

                try
                {
                    (var ao, var co) = WorldDownloaderNew.PrepareGetWorldInfo(cid);
                    yield return ao.ExecuteCoroutine(co);

                    AddListEntry(cid, true);

                    yield return ShowPage(0);
                }
                finally
                {
                    btn_AddWorld.interactable = true;

                }
            }

            StartCoroutine(ParseWorldCoroutine());
        }
    }
}
