/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Services;
using Ipfs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Arteranos.Core
{
    public partial class WorldInfo : FlatFileDB<WorldInfo>
    {
        public WorldInfo() 
        {
            _KnownPeersRoot = $"{FileUtils.persistentDataPath}/WorldInfos";
            _GetFileName = cid => $"{FileUtils.persistentDataPath}/WorldInfos/{Utils.GetURLHash(cid)}.info";
            _SearchPattern = "*.info";
            _Deserialize = Deserialize;
            _Serialize = Serialize;
        }

        public bool DBUpdate()
            => _DBUpdate(WorldCid, old => old.Updated <= Updated);

        [Obsolete("TODO Clarify: Meta Info Cid")]
        public static WorldInfo DBLookup(Cid cid)
            => new WorldInfo()._DBLookup(cid);

        [Obsolete("TODO Clarify: Meta Info Cid")]
        public static void DBDelete(Cid cid)
        {
            new WorldInfo()._DBDelete(cid);
            // Remove the pin, too, just in case.
            IPFSService.Ipfs.Pin.RemoveAsync(cid);
        }

        public static IEnumerable<WorldInfo> DBList()
            => new WorldInfo()._DBList();

        // ---------------------------------------------------------------

        public void Favourite()
        {
            IPFSService.Ipfs.Pin.AddAsync(WorldCid);
        }

        public void Unfavourite()
        {
            IPFSService.Ipfs.Pin.RemoveAsync(WorldCid);
        }

        public bool IsFavourited()
        {
            List<Cid> all = Task.Run(async () => (await IPFSService.Ipfs.Pin.ListAsync()).ToList()).Result;
            return all.Contains(WorldCid);
        }


        public void BumpWI()
        {
            Updated = DateTime.Now;
            DBUpdate();
        }
    }
}