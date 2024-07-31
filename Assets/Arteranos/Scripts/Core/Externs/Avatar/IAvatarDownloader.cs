/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using Ipfs;
using UnityEngine;

namespace Arteranos.Avatar
{
    public interface IAvatarDownloader
    {
        string GetAvatarCacheFile(Cid cid);
        IAvatarMeasures GetAvatarMeasures(Context _context);
        IObjectStats GetAvatarRating(Context _context);
        GameObject GetLoadedAvatar(Context _context);
        (AsyncOperationExecutor<Context>, Context) PrepareDownloadAvatar(Cid cid, IAvatarDownloaderOptions options = null, int timeout = 600);
    }
}