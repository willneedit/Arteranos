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

using System.Threading;
using Ipfs;
using Arteranos.Services;
using System.Collections.Generic;
using System.Text;
using ICSharpCode.SharpZipLib.Tar;

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

            byte[] screenshotBytes = null;
            using (Stream stream = await IPFSService.ReadFile($"{context.WorldCid}/{screenshotName}"))
            {
                using MemoryStream ms = new();
                await Utils.CopyWithProgress(stream, ms);
                screenshotBytes = ms.ToArray();
            }

            using (Stream stream = await IPFSService.ReadFile($"{context.WorldCid}/Metadata.json"))
            {
                using MemoryStream ms = new();
                await Utils.CopyWithProgress(stream, ms);
                string json = Encoding.UTF8.GetString(ms.ToArray());
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
            }

            return context;
        }
    }

    internal class DownloadWorldDataOp : IAsyncOperation<Context>
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

            WorldDownloadContext context = _context as WorldDownloadContext;

            // Invalidate the 'current' asset bundle path.
            WorldDownloader.CurrentWorldAssetBundlePath = null;

            string assetPath = $"{context.WorldCid}/{GetArchitectureDirName()}";

            IFileSystemNode fi = await IPFSService.ListFile(assetPath, token);
            if (!fi.IsDirectory)
                throw new InvalidDataException("World data packet is not a directory");

            IEnumerable<IFileSystemLink> files = fi.Links;
            foreach (IFileSystemLink file in files) totalBytes += file.Size;
            totalBytesMag = Utils.Magnitude(totalBytes);

            // Clean out the unpacked files - IPFS takes care of the world data with its
            // sense of importance (pimmed/unpinned like favourited/unfavourited)
            Directory.Delete(Utils.WorldCacheRootDir, true);
            Directory.CreateDirectory(Utils.WorldCacheRootDir);

            Stream tar = await IPFSService.Get(assetPath, token);

            // Worker task to observe the progress...
            using CancellationTokenSource cts = new();
            _ = Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    if(actualBytes != tar.Position)
                    {
                        actualBytes = tar.Position;
                        ProgressChanged((float)actualBytes / totalBytes);
                    }
                    await Task.Delay(10);
                }
            });

            using TarArchive archive = TarArchive.CreateInputTarArchive(tar, Encoding.UTF8);
            archive.ExtractContents(Utils.WorldCacheRootDir);
            cts.Cancel(); // ... and he's done it.

            context.WorldAssetBundlePath = null;
            foreach (string file in Directory.EnumerateFiles($"{Utils.WorldCacheRootDir}/{fi.Id}", "*.unity"))
            {
                context.WorldAssetBundlePath = file;
                break;
            }

            if (context.WorldAssetBundlePath == null)
                throw new FileNotFoundException("World Asset Bundle not found");

            WorldDownloader.CurrentWorldAssetBundlePath = context.WorldAssetBundlePath;
            return context;
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

        public static (AsyncOperationExecutor<Context>, Context) PrepareGetWorldAsset(Cid WorldCid, int timeout = 600)
        {
            WorldDownloadContext context = new()
            {
                WorldCid = WorldCid
            };

            AsyncOperationExecutor<Context> executor = new(new IAsyncOperation<Context>[]
            {
                new DownloadWorldDataOp(),
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
