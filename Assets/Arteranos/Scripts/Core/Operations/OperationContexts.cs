/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using UnityEngine;

using Arteranos.Core;
using System.Threading;
using Utils = Arteranos.Core.Utils;
using Ipfs;
using Arteranos.Services;
using System.Collections.Generic;

namespace Arteranos.Core.Operations
{
    internal class AssetDownloaderContext : Context
    {
        public Cid Cid = null;
        public string TargetFile = null;
        public long size = -1;
    }

    internal class WorldDownloaderContext : AssetDownloaderContext
    {
        public Cid WorldInfoCid = null;
        public string worldAssetBundleFile = null;
    }

    internal class ServerSearcherContext : Context
    {
        public List<ServerInfo> serverInfos = null;
        public Cid desiredWorldCid = null;
        public ServerPermissions desiredWorldPermissions = null;
        public MultiHash resultPeerID = null;
    }

    internal class AssetUploaderContext : Context
    {
        public string AssetURL = null;
        public string TempFile = null;
        public bool pin = false;
        public Cid Cid = null;
    }
}
