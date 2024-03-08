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

namespace Arteranos.Core.Operations
{
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
                string wcd = WorldDownloader.GetWorldCacheDir(context.path);
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
                        WorldCid = context.path,
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
                string path = Path.ChangeExtension(context.TargetFile, "dir");

                if (Directory.Exists(path)) Directory.Delete(path, true);

                ZipFile.ExtractToDirectory(context.TargetFile, path);

                string worldABF = WorldDownloader.GetWorldABFfromWD(path);

                context.worldAssetBundleFile = worldABF;

                return context;

            });
        }
    }
    
    public static class WorldDownloader
    {
        public static (AsyncOperationExecutor<Context>, Context) PrepareDownloadWorld(Cid cid, int timeout = 600)
        {
            WorldDownloaderContext context = new()
            {
                path = cid,
                TargetFile = $"{GetWorldCacheDir(cid)}/world.zip"
            };


            AsyncOperationExecutor<Context> executor = new(new IAsyncOperation<Context>[]
            {
                new AssetDownloadOp(),
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
    }
}
