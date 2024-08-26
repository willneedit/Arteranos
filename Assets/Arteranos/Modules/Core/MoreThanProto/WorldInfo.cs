/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core.Operations;
using Arteranos.Services;
using Ipfs;
using Ipfs.CoreApi;
using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Arteranos.Core
{
    public partial class WorldInfo : FlatFileDB<WorldInfo>
    {
        // Convenience redirections.
        public string WorldCid => win.WorldCid;
        public string WorldName => win.WorldName;
        public string WorldDescription => win.WorldDescription;
        public ServerPermissions ContentRating => win.ContentRating;

        public WorldInfo()
        {
            Init();
        }

        private void Init()
        {
            _KnownPeersRoot = $"{FileUtils.persistentDataPath}/WorldInfos";
            _GetFileName = cid => $"{FileUtils.persistentDataPath}/WorldInfos/{Utils.GetURLHash(cid)}.info";
            _SearchPattern = "*.info";
            _Deserialize = Deserialize;
            _Serialize = Serialize;
        }

        public bool DBUpdate()
            => _DBUpdate(WorldCid, old => old.Updated < Updated);

        public static WorldInfo DBLookup(Cid cid) 
            => new WorldInfo()._DBLookup(cid);

        public static void DBDelete(Cid cid)
        {
            new WorldInfo()._DBDelete(cid);
            // Remove the pin, too, just in case.
            _ = G.IPFSService.PinCid(cid, false);
        }

        public static IEnumerable<WorldInfo> DBList()
            => new WorldInfo()._DBList();

        public static IEnumerator RetrieveCoroutine(Cid WorldCid, Action<WorldInfo> callback)
        {
            if(WorldCid == null)
            {
                callback?.Invoke(null);
                yield break;
            }

            (AsyncOperationExecutor<Context> ao, Context co) = WorldDownloader.PrepareGetWorldInfo(WorldCid);

            yield return ao.ExecuteCoroutine(co, (status, co) => {
                if(co != null) callback?.Invoke(WorldDownloader.GetWorldInfo(co));
            });
        }

        // ---------------------------------------------------------------

        public void Favourite()
        {
            Client cs = G.Client;
            if (!cs.FavouritedWorlds.Contains(WorldCid))
                cs.FavouritedWorlds.Add(WorldCid);

            cs.Save();
        }

        public void Unfavourite()
        {
            Client cs = G.Client;
            cs.FavouritedWorlds.Remove(WorldCid);

            cs.Save();
        }

        public static List<Cid> ListFavourites()
        {
            return G.Client.FavouritedWorlds;
        }

        public void BumpWI()
        {
            Updated = DateTime.Now;
            DBUpdate();
        }
    }
}