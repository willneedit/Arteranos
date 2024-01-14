/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Ipfs;
using System.Collections.Generic;

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

        public static WorldInfo DBLookup(Cid cid)
            => _DBLookup(cid);

        public static void DBDelete(Cid cid)
            => _DBDelete(cid);

        public static IEnumerable<WorldInfo> DBList()
            => _DBList();
    }
}