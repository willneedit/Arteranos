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
        [SerializeField] private ObjectChooser Chooser;

        private readonly ConcurrentDictionary<Cid, Collection> worldlist = new();
        private readonly List<Cid> sortedWorldList = new();
        private Mutex DictMutex = null;


        public static WorldPanelUI New()
        {
            GameObject go = Instantiate(BP.I.UI.WorldPanel);
            return go.GetComponent<WorldPanelUI>();
        }

        protected override void Awake()
        {
            base.Awake();

            DictMutex = new();

            Chooser.OnShowingPage += PreparePage;
            Chooser.OnPopulateTile += PopulateTile;
            Chooser.OnAddingItem += RequestToAdd;
        }

        private void PreparePage(int obj)
        {
            Chooser.UpdateItemCount(sortedWorldList.Count);
        }

        private void RequestToAdd(string obj)
        {
            CancellationTokenSource cts = new();


            IProgressUI pui = Factory.NewProgress();

            pui.SetupAsyncOperations(() => AssetUploader.PrepareUploadToIPFS(obj, true, pin: true)); // World Zip file

            pui.Completed += context =>
            {
                Cid cid = AssetUploader.GetUploadedCid(context);
                ParseWorld(cid);
                Chooser.FinishAdding();
            };

            pui.Faulted += (ex, context) =>
            {
                Chooser.FinishAdding();
                ShowPage(0);
            };
        }

        private void PopulateTile(int i, GameObject @object)
        {
            if (!@object.TryGetComponent(out WorldPaneltem wli)) return;

            wli.WorldCid = sortedWorldList[i];
            if (worldlist.TryGetValue(wli.WorldCid, out Collection list))
            {
                wli.ServersCount = list.serversCount;
                wli.UsersCount = list.usersCount;
                wli.FriendsMax = list.friendsMax;
                wli.Favourited = list.favourited;
            }
        }

        protected override void OnDestroy()
        {
            Chooser.OnPopulateTile -= PopulateTile;
            Chooser.OnAddingItem -= RequestToAdd;
            Chooser.OnShowingPage -= PreparePage;

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

                Chooser.ShowPage(0);
            }

            base.Start();

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

        private void ShowPage(int currentPage) => Chooser.ShowPage(currentPage);


        private void ParseWorld(Cid cid)
        {
            IEnumerator ParseWorldCoroutine()
            {
                yield return null;

                (var ao, var co) = WorldDownloader.PrepareGetWorldInfo(cid);
                yield return ao.ExecuteCoroutine(co);

                yield return AddManualWorldCoroutine(cid, true, null);

                CreateSortedWorldList();

                ShowPage(0);
            }

            StartCoroutine(ParseWorldCoroutine());
        }
    }
}
