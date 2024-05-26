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
using System.Threading;
using System.Collections;
using Ipfs;
using Arteranos.Core.Operations;
using System.Collections.Concurrent;

namespace Arteranos.UI
{
    internal class Collection
    {
        public Cid worldCid;
        public int serversCount;
        public int usersCount;
        public int friendsMax;
        public bool favourited;
        public bool current;
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

        private readonly ConcurrentDictionary<Cid, Collection> worldlist = new();
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
            IEnumerator BuildSortedList()
            {
                worldlist.Clear();

                if(SettingsManager.WorldCid != null)
                    yield return AddManualWorldCoroutine(SettingsManager.WorldCid, false, true);

                yield return GatherServeredWorlds();

                yield return GatherFavouritedWorlds();

                CreateSortedWorldList();

                yield return ShowPage(0);

            }

            base.Start();

            lbl_PageCount.text = "Loading...";

            StartCoroutine(BuildSortedList());
        }

        private IEnumerator GatherServeredWorlds()
        {
            foreach(ServerInfo si in ServerInfo.Dump(DateTime.MinValue))
            {
                if(si.CurrentWorldCid == null) continue;

                worldlist.AddOrUpdate(si.CurrentWorldCid, new Collection()
                {
                    favourited = false,
                    current = false,
                    friendsMax = 0,
                    serversCount = 1,
                    usersCount = si.UserCount,
                    worldCid = si.CurrentWorldCid
                },
                (cid, coll) =>
                {
                    coll.friendsMax = si.FriendCount > coll.friendsMax ? si.FriendCount : coll.friendsMax;
                    coll.serversCount++;
                    coll.usersCount += si.UserCount;
                    return coll;
                });

                yield return new WaitForEndOfFrame();
            }
        }

        private IEnumerator GatherFavouritedWorlds()
        {
            List<Cid> pinned = WorldInfo.ListFavourites();

            foreach(Cid cid in pinned)
            {
                yield return AddManualWorldCoroutine(cid, true, null);

                yield return new WaitForEndOfFrame();
            }
        }

        private IEnumerator AddManualWorldCoroutine(Cid WorldCid, bool? favourited, bool? current)
        {
            worldlist.AddOrUpdate(WorldCid, new Collection()
            {
                favourited = favourited ?? true,
                current = current ?? false,
                friendsMax = 0,
                serversCount = 0,
                usersCount = 0,
                worldCid = WorldCid
            },
            (cid, coll) =>
            {
                coll.favourited = favourited ?? coll.favourited;
                coll.current = current ?? coll.current;
                return coll;
            });

            yield return new WaitForEndOfFrame();
        }

        private void CreateSortedWorldList()
        {
            sortedWorldList.Clear();
            foreach(KeyValuePair<Cid, Collection> item in worldlist)
            {
                // It's nowhere hosted and unfavourited, so leave out the dross
                if (ScoreWorld(item.Key) <= 0) continue;

                sortedWorldList.Add(item.Key);
            }

            sortedWorldList.Sort((x, y) => ScoreWorld(y) - ScoreWorld(x));
        }

        private void AddManualWorld(Cid WorldCid)
        {
            IEnumerator DoAdd()
            {
                yield return AddManualWorldCoroutine(WorldCid, true, null);

                CreateSortedWorldList();

                yield return ShowPage(0);
            }

            StartCoroutine(DoAdd());
        }

        private int ScoreWorld(Cid cid) 
        {
            if(!worldlist.TryGetValue(cid, out Collection list)) return -10000;

            int score = 0;

            score += list.serversCount; // Servers get one point.

            score += list.usersCount * 5; // Users get five points.

            score += list.friendsMax * 20; // Friends get twenty points.

            score += list.favourited ? 100000 : 0; // A class for its own.

            score += list.current ? 100000 : 0; // Always first.

            return score;
        }

        private IEnumerator ShowPage(int currentPage)
        {
            IEnumerator PopupPanel(Transform panels, int i)
            {
                GameObject go = Instantiate(grp_WorldPanelSample, panels);
                WorldPaneltem wli = go.GetComponentInChildren<WorldPaneltem>();
                wli.WorldCid = sortedWorldList[i];
                if (worldlist.TryGetValue(wli.WorldCid, out Collection list))
                {
                    wli.ServersCount = list.serversCount;
                    wli.UsersCount = list.usersCount;
                    wli.FriendsMax = list.friendsMax;
                    wli.Favourited = list.favourited;
                }
                go.SetActive(true);

                yield return new WaitForEndOfFrame();
            }

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
                StartCoroutine(PopupPanel(panels, i));

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
                    (var ao, var co) = WorldDownloader.PrepareGetWorldInfo(cid);
                    yield return ao.ExecuteCoroutine(co);

                    AddManualWorld(cid);

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
