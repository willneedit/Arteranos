/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Ipfs;
using Arteranos.Services;
using UnityEngine;
using System.Collections.Generic;

namespace Arteranos.Core.Operations
{
    public static class OpUtils
    {
        /// <summary>
        /// Find the matching asset bundle within the who world/kit archive
        /// </summary>
        /// <param name="archiveCid">root Cid of the archive, containing all thge architecture asset bundles and additional data</param>
        /// <param name="token"></param>
        /// <returns>Cid, Name, Size</returns>
        /// <exception cref="InvalidDataException">archiveCid points to a malformed archive (e.g. a Zip archive, not a directory)</exception>
        /// <exception cref="FileNotFoundException">archive has no matching assetBundle</exception>
        public static async Task<IFileSystemLink> ExtractAssetArchive(Cid archiveCid, CancellationToken token)
        {
            string assetPath = $"{archiveCid}/{GetArchitectureDirName()}";

            // HACK: Kubo's ListFiles doesn't implicitly resolve.
            assetPath = await IPFSService.ResolveToCid(assetPath, token);

            IFileSystemNode fi = await IPFSService.ListFile(assetPath, token);
            if (!fi.IsDirectory)
                throw new InvalidDataException("Asset Archive is not a directory");

            foreach (IFileSystemLink file in fi.Links)
                if (file.Name.EndsWith(".unity"))
                    return file;

            throw new FileNotFoundException("No usable Asset Bundle found");
        }

        public static string GetArchitectureDirName()
        {
            string archPath = "AssetBundles";
            RuntimePlatform p = Application.platform;
            if (p == RuntimePlatform.OSXEditor || p == RuntimePlatform.OSXPlayer)
                archPath = "Mac";
            if (p == RuntimePlatform.Android)
                archPath = "Android";

            return archPath;
        }

    }
}