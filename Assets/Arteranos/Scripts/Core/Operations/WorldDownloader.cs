/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

using System.Threading;
using Ipfs;
using Arteranos.Services;
using System.Collections.Generic;
using System.Text;

namespace Arteranos.Core.Operations
{
    internal class DownloadWorldInfoOp : IAsyncOperation<Context>
    {
        public int Timeout { get; set; }
        public float Weight { get; set; } = 2.0f;
        public string Caption { get; set; } = "Uncompressing file";
        public Action<float> ProgressChanged { get; set; }

        public async Task<Context> ExecuteAsync(Context _context, CancellationToken token)
        {
            WorldDownloadContext context = _context as WorldDownloadContext;

            IFileSystemNode fsn = await IPFSService.ListFile(context.WorldCid);
            IEnumerable<IFileSystemLink> links = fsn.Links;

            string screenshotName = null;
            long screenshotSize = 0;

            foreach(IFileSystemLink link in links)
                if (link.Name.StartsWith("Screenshot"))
                {
                    screenshotName = link.Name;
                    screenshotSize = link.Size;
                    break;
                }

            byte[] screenshotBytes = await IPFSService.ReadBinary($"{context.WorldCid}/{screenshotName}");

            byte[] mdbytes = await IPFSService.ReadBinary($"{context.WorldCid}/Metadata.json");

            string json = Encoding.UTF8.GetString(mdbytes);
            WorldMetaData metaData = WorldMetaData.Deserialize(json);

            WorldInfo wi = new()
            {
                win = new()
                {
                    WorldCid = context.WorldCid,
                    WorldName = metaData.WorldName,
                    WorldDescription = metaData.WorldDescription,
                    Author = metaData.AuthorID,
                    ContentRating = metaData.ContentRating,
                    Signature = null,
                    ScreenshotPNG = screenshotBytes,
                    Created = metaData.Created,
                },
                Updated = DateTime.MinValue
            };
            context.WorldInfo = wi;
            wi.DBUpdate();

            return context;
        }
    }

    internal class DownloadTemplateOp : IAsyncOperation<Context>
    {
        public int Timeout { get; set; }
        public float Weight { get; set; } = 8.0f;
        public string Caption { get => GetProgressText(); }
        public Action<float> ProgressChanged { get; set; }

        private long actualBytes = 0;
        private long totalBytes = 0;
        private string totalBytesMag = null;

        private string GetProgressText()
        {
            if (totalBytesMag == null) return "Downloading...";

            return $"Downloading ({Utils.Magnitude(actualBytes)} of {totalBytesMag})...";
        }

        public async Task<Context> ExecuteAsync(Context _context, CancellationToken token)
        {

            WorldDownloadContext context = _context as WorldDownloadContext;

            // Invalidate the 'current' asset bundle path.
            WorldDownloader.CurrentWorldAssetBundlePath = null;

            IFileSystemLink found = await ExtractAssetArchive(context.WorldCid, token);

            totalBytes = found.Size;
            totalBytesMag = Utils.Magnitude(totalBytes);
            Cid assetBundleCid = found.Id;

            // Clean out the unpacked files - IPFS takes care of the world data with its
            // sense of importance (pinned/unpinned like favourited/unfavourited)
            if (Directory.Exists(Utils.WorldCacheRootDir)) Directory.Delete(Utils.WorldCacheRootDir, true);
            Directory.CreateDirectory(Utils.WorldCacheRootDir);
            context.WorldAssetBundlePath = $"{Utils.WorldCacheRootDir}/{assetBundleCid}";

            using Stream instr = await IPFSService.ReadFile(assetBundleCid, token);
            using Stream outstr = File.Create(context.WorldAssetBundlePath);

            await Utils.CopyWithProgress(instr, outstr, _actual =>
            {
                actualBytes = _actual;
                ProgressChanged((float)_actual / totalBytes);
            });

            WorldDownloader.CurrentWorldAssetBundlePath = context.WorldAssetBundlePath;
            return context;
        }

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
            static string GetArchitectureDirName()
            {
                string archPath = "AssetBundles";
                RuntimePlatform p = Application.platform;
                if (p == RuntimePlatform.OSXEditor || p == RuntimePlatform.OSXPlayer)
                    archPath = "Mac";
                if (p == RuntimePlatform.Android)
                    archPath = "Android";

                return archPath;
            }

            string assetPath = $"{archiveCid}/{GetArchitectureDirName()}";

            // HACK: Kubo's ListFiles doesn't implicitly resolve.
            assetPath = await IPFSService.ResolveToCid(assetPath, token);

            IFileSystemNode fi = await IPFSService.ListFile(assetPath, token);
            if (!fi.IsDirectory)
                throw new InvalidDataException("Asset Archive is not a directory");

            IEnumerable<IFileSystemLink> files = fi.Links;

            IFileSystemLink found = null;
            foreach (IFileSystemLink file in files)
                if (file.Name.EndsWith(".unity"))
                {
                    found = file;
                    break;
                }

            if (found == null)
                throw new FileNotFoundException("No usable Asset Bundle found");

            return found;
        }
    }
 
    public static class WorldDownloader
    {
        public static string CurrentWorldAssetBundlePath { get; internal set; } = null;

        public static (AsyncOperationExecutor<Context>, Context) PrepareGetWorldInfo(Cid WorldCid, int timeout = 600)
        {
            WorldDownloadContext context = new()
            {
                WorldCid = WorldCid
            };

            AsyncOperationExecutor<Context> executor = new(new IAsyncOperation<Context>[]
            {
                new DownloadWorldInfoOp()
            })
            {
                Timeout = timeout
            };

            return (executor, context);
        }

        public static (AsyncOperationExecutor<Context>, Context) PrepareGetWorldTemplate(Cid WorldCid, int timeout = 600)
        {
            WorldDownloadContext context = new()
            {
                WorldCid = WorldCid
            };

            AsyncOperationExecutor<Context> executor = new(new IAsyncOperation<Context>[]
            {
                // TODO #115: Before: Check type: pure template or decorated world data (template + object list)
                new DownloadTemplateOp(),
            })
            {
                Timeout = timeout
            };

            return (executor, context);
        }

        public static WorldInfo GetWorldInfo(Context _context)
            => (_context as WorldDownloadContext).WorldInfo;

        public static string GetWorldDataFile(Context _context)
            => (_context as WorldDownloadContext).WorldAssetBundlePath;
    }
}
