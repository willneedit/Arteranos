/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

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
            IPFSService.Ipfs.Pin.RemoveAsync(cid);
        }

        public static IEnumerable<WorldInfo> DBList()
            => new WorldInfo()._DBList();

        public static async Task<WorldInfo> RetrieveAsync(Cid WorldInfoCid, CancellationToken cancel = default)
        {
            if (WorldInfoCid == null) return null;

            try
            {
                Stream s = await IPFSService.Ipfs.FileSystem.ReadFileAsync(WorldInfoCid, cancel);
                WorldInfo wi = new()
                {
                    win = null,
                    WorldInfoCid = WorldInfoCid,
                    Updated = DateTime.MinValue
                };

                wi.win = Serializer.Deserialize<WorldInfoNetwork>(s);
                return wi;
            }
            catch
            {
                return null;
            }
        }

        public static WorldInfo Retrieve(Cid WorldInfoCid) 
            => Task.Run(async () => await RetrieveAsync(WorldInfoCid)).Result;

        public async Task<Cid> PublishAsync(bool dryRun = false, CancellationToken cancel = default)
        {
            AddFileOptions ao = null;
            if (dryRun)
                ao = new AddFileOptions() { OnlyHash = true };

            using MemoryStream ms = new();
            Serializer.Serialize(ms, win);
            ms.Position = 0;

            IFileSystemNode fsn = await IPFSService.Ipfs.FileSystem.AddAsync(ms, options: ao, cancel: cancel);
            WorldInfoCid = fsn.Id;
            return WorldInfoCid;
        }

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