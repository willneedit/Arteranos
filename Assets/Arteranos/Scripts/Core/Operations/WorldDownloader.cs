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

namespace Arteranos.Core.Operations
{
    internal class WorldDownloaderContext : Context
    {
        public Cid Cid = null;
        public Cid WorldInfoCid = null;
        public long size = -1;
        public string worldZipFile = null;
        public string worldAssetBundleFile = null;
    }

    internal class BuildWorldInfoOp : IAsyncOperation<Context>
    {
        public int Timeout { get; set; }
        public float Weight { get; set; } = 0.5f;
        public string Caption => "Caching...";
        public Action<float> ProgressChanged { get; set; }

        public async Task<Context> ExecuteAsync(Context _context, CancellationToken token)
        {
            WorldDownloaderContext context = _context as WorldDownloaderContext;


            async Task<Context> Execute()
            {
                string wcd = WorldDownloader.GetWorldCacheDir(context.Cid);
                string rootPath = $"{wcd}/world.dir";

                string metadataFile = $"{rootPath}/Metadata.json";
                string screenshotFile = null;
                foreach (string file in Directory.EnumerateFiles(rootPath, "Screenshot.*"))
                {
                    screenshotFile = $"{rootPath}/{Path.GetFileName(file)}";
                    break;
                }

                string json = File.ReadAllText(metadataFile);
                byte[] screenshotBytes = File.ReadAllBytes(screenshotFile);

                WorldMetaData metaData = WorldMetaData.Deserialize(json);

                WorldInfo wi = new()
                {
                    win = new()
                    {
                        WorldCid = context.Cid,
                        WorldName = metaData.WorldName,
                        WorldDescription = metaData.WorldDescription,
                        AuthorNickname = (string)metaData.AuthorID,
                        AuthorPublicKey = (byte[])metaData.AuthorID,
                        ContentRating = metaData.ContentRating,
                        Signature = null,
                        ScreenshotPNG = screenshotBytes,
                        Created = metaData.Created,
                    },
                    Updated = DateTime.MinValue
                };
                context.WorldInfoCid = await wi.PublishAsync();
                wi.DBUpdate();

                return context;
            }

            return await Task.Run(Execute);
        }
    }
    internal class UnzipWorldFileOp : IAsyncOperation<Context>
    {
        public int Timeout { get; set; }
        public float Weight { get; set; } = 2.0f;
        public string Caption { get; set; } = "Uncompressing file";
        public Action<float> ProgressChanged { get; set; }

        public async Task<Context> ExecuteAsync(Context _context, CancellationToken token)
        {
            WorldDownloaderContext context = _context as WorldDownloaderContext;

            return await Task.Run(() =>
            {
                string path = Path.ChangeExtension(context.worldZipFile, "dir");

                if (Directory.Exists(path)) Directory.Delete(path, true);

                ZipFile.ExtractToDirectory(context.worldZipFile, path);

                string worldABF = WorldDownloader.GetWorldABFfromWD(path);

                context.worldAssetBundleFile = worldABF;

                return context;

            });
        }
    }

    internal class DownloadOp : IAsyncOperation<Context>
    {
        public int Timeout { get; set; }
        public float Weight { get; set; } = 8.0f;
        public string Caption { get => GetProgressText(); }
        public Action<float> ProgressChanged { get; set; }

        private long actualBytes = 0;
        private long totalBytes = -1;
        private string totalBytesMag = null;

        private string GetProgressText()
        {
            if (totalBytesMag == null || totalBytes <= 0) return "Downloading...";

            return $"Downloading ({Utils.Magnitude(actualBytes)} of {totalBytesMag})...";
        }

        public async Task<Context> ExecuteAsync(Context _context, CancellationToken token)
        {
            WorldDownloaderContext context = _context as WorldDownloaderContext;

            context.worldZipFile = $"{WorldDownloader.GetWorldCacheDir(context.Cid)}/world.zip";

            IDataBlock fi = await IPFSService.Ipfs.FileSystem.ListFileAsync(context.Cid, token);

            totalBytes = fi.Size;
            totalBytesMag = Utils.Magnitude(totalBytes);

            using Stream inStream = await IPFSService.Ipfs.FileSystem.ReadFileAsync(context.Cid, cancel: token);

            string dir = Path.GetDirectoryName(context.worldZipFile);
            if(!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            using FileStream outStream = File.Create(context.worldZipFile);

            await Utils.CopyWithProgress(inStream, outStream,
                bytes => {
                    actualBytes = bytes;
                    ProgressChanged((float)bytes / totalBytes);
                }, token);

            return context;
        }
    }
    
    public static class WorldDownloader
    {
        public static (AsyncOperationExecutor<Context>, Context) PrepareDownloadWorld(Cid cid, int timeout = 600)
        {
            WorldDownloaderContext context = new()
            {
                Cid = cid
            };


            AsyncOperationExecutor<Context> executor = new(new IAsyncOperation<Context>[]
            {
                new DownloadOp(),
                new UnzipWorldFileOp(),
                new BuildWorldInfoOp(),
            })
            {
                Timeout = timeout
            };

            return (executor, context);

        }

        public static void EnterDownloadedWorld(Context _context)
        {
            string worldABF = GetWorldABF(_context);

            WorldTransition.EnterDownloadedWorld(worldABF);
        }

        private static string GetWorldABF(Context _context) 
            => (_context as WorldDownloaderContext).worldAssetBundleFile;

        public static Cid GetWorldInfoCid(Context _context)
            => (_context as WorldDownloaderContext).WorldInfoCid;

        public static Task<WorldInfo> GetWorldInfoAsync(Context _context) 
            => WorldInfo.RetrieveAsync(GetWorldInfoCid(_context));

        public static string GetWorldABF(Cid cid) 
            => GetWorldABFfromWD($"{GetWorldCacheDir(cid)}/world.dir");

        public static string GetWorldCacheDir(Cid cid) 
            => $"{Utils.WorldCacheRootDir}/{Utils.GetURLHash(cid)}";

        public static string GetWorldABFfromWD(string path)
        {
            string archPath = "AssetBundles";
            RuntimePlatform p = Application.platform;
            if (p == RuntimePlatform.OSXEditor || p == RuntimePlatform.OSXPlayer)
                archPath = "Mac";
            if (p == RuntimePlatform.Android)
                archPath = "Android";

            path += "/" + archPath;

            if (!Directory.Exists(path))
                throw new FileNotFoundException($"No {archPath} directory in the world zip file");

            string worldABF = null;
            foreach (string file in Directory.EnumerateFiles(path, "*.unity"))
            {
                worldABF = file;
                break;
            }

            if (worldABF == null)
                throw new FileNotFoundException("No suitable AssetBundle found in the zipfile.");
            return worldABF;
        }


        //private void OnEnable()
        //{
        //    DownloadWorldAsync(
        //        "https://github.com/willneedit/willneedit.github.io/blob/master/Abbey.zip?raw=true"
        //        // "file://C:/Users/willneedit/Desktop/world.zip"
        //        );

        //    Debug.Log("Spawned the downloader in the background, we'll see...");
        //}
    }
}
