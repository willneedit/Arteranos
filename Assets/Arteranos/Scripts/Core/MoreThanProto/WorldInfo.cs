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
            IPFSService.PinCid(cid, false);
        }

        public static IEnumerable<WorldInfo> DBList()
            => new WorldInfo()._DBList();

        public static async Task<WorldInfo> RetrieveAsync(Cid WorldCid)
        {
            // null means the offline world... or falling back to the erroneous world.
            if (WorldCid == null) return null;

            try
            {
                (AsyncOperationExecutor<Context> ao, Context co) =
                    WorldDownloader.PrepareGetWorldInfo(WorldCid);

                co = await ao.ExecuteAsync(co);

                WorldInfo wi = WorldDownloader.GetWorldInfo(co);
                return wi;
            }
            catch
            {
                return null;
            }
        }

        // ---------------------------------------------------------------

        public void Favourite()
        {
            IPFSService.PinCid(WorldCid, true);
        }

        public void Unfavourite()
        {
            IPFSService.PinCid(WorldCid, false);
        }

        public bool IsFavourited()
        {
            IEnumerable<Cid> all = Task.Run(() => IPFSService.ListPinned()).Result;
            return all.ToList().Contains(WorldCid);
        }


        public void BumpWI()
        {
            Updated = DateTime.Now;
            DBUpdate();
        }
    }
}