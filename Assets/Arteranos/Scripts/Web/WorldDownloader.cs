/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

using Arteranos.Core;
using System.Threading;
using Utils = Arteranos.Core.Utils;

namespace Arteranos.Web
{
    internal class WorldDownloaderContext : Context
    {
        public string url = null;
        public string hashed = null;
        public string targetfile = null;
        public bool reload = false;
        public string cachedir = null;
        public bool cacheHit = false;
        public DateTime lastModified = DateTime.MinValue;
        public long size = -1;
        public string worldZipFile = null;
        public string worldAssetBundleFile = null;
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

                if(Directory.Exists(path))
                    Directory.Delete(path, true);

                ZipFile.ExtractToDirectory(context.worldZipFile, path);

                string archPath = "AssetBundles";
                RuntimePlatform p = Application.platform;
                if(p == RuntimePlatform.OSXEditor || p == RuntimePlatform.OSXPlayer)
                    archPath = "Mac";
                if(p == RuntimePlatform.Android)
                    archPath = "Android";

                path += "/" + archPath;

                if(!Directory.Exists(path))
                    throw new FileNotFoundException($"No {archPath} directory in {context.worldZipFile}");


                foreach(string file in Directory.EnumerateFiles(path, "*.unity"))
                {
                    context.worldAssetBundleFile = file;
                    return context;
                }

                throw new FileNotFoundException("No suitable AssetBundle found in the zipfile.");
            });
        }
    }

    internal class DownloadOp : IAsyncOperation<Context>
    {
        public int Timeout { get; set; }
        public float Weight { get; set; } = 8.0f;
        public string Caption { 
            get => "Downloading...";
            set { }
        }

        public Action<float> ProgressChanged { get; set; }

        public async Task<Context> ExecuteAsync(Context _context, CancellationToken token)
        {
            WorldDownloaderContext context = _context as WorldDownloaderContext;

            if(context.cacheHit) return context;

            using UnityWebRequest uwr = new(context.url, UnityWebRequest.kHttpVerbGET);

            uwr.timeout = Timeout;
            uwr.downloadHandler = new DownloadHandlerFile(context.worldZipFile);

            UnityWebRequestAsyncOperation uwr_ao = uwr.SendWebRequest();

            while(!uwr_ao.isDone && !token.IsCancellationRequested)
            {
                await Task.Yield();
                ProgressChanged?.Invoke(uwr.downloadProgress);
            }

            if(uwr.result == UnityWebRequest.Result.ProtocolError || uwr.result == UnityWebRequest.Result.ConnectionError)
                throw new FileNotFoundException(uwr.error, context.worldZipFile);

            return context;
        }
    }

    internal class CheckCacheOp : IAsyncOperation<Context>
    {
        public int Timeout { get; set; }
        public float Weight { get; set; } = 1.0f;
        public string Caption
        {
            get => "Connecting";
            set { }
        }

        public Action<float> ProgressChanged { get; set; }

        public async Task<Context> ExecuteAsync(Context _context, CancellationToken token)
        {
            WorldDownloaderContext context = _context as WorldDownloaderContext;

            context.hashed = Utils.GetURLHash(context.url);
            context.cachedir = $"{Utils.WorldCacheRootDir}/{context.hashed}";
            context.worldZipFile = $"{context.cachedir}/{context.targetfile}";

            if(context.reload)
            {
                context.cacheHit = false;
                return context;
            }

            if(!File.Exists(context.worldZipFile))
            {
                context.cacheHit = false;
                return context;
            }

            FileInfo fi = new(context.worldZipFile);

            DateTime locDT = fi.LastWriteTime;
            long locSize = fi.Length;

            // Last write time is younger than 10 minutes; reduce network load
            // and require a forced cache flush.
            if((DateTime.Now - locDT) < TimeSpan.FromMinutes(10))
            {
                context.cacheHit = true;
                return context;
            }

            using UnityWebRequest uwr = new(context.url, UnityWebRequest.kHttpVerbHEAD);
            UnityWebRequestAsyncOperation uwr_ao = uwr.SendWebRequest();

            while(!uwr_ao.isDone && !token.IsCancellationRequested)
            {
                await Task.Yield();
                ProgressChanged?.Invoke(uwr.downloadProgress);
            }

            // No network response, but we have a local file to work with it.
            if(uwr.result != UnityWebRequest.Result.Success)
            {
                context.cacheHit = true;
                return context;
            }

            string lmstr = uwr.GetResponseHeader("Last-Modified");
            context.lastModified = (lmstr != null) ? DateTime.Parse(lmstr) : DateTime.UnixEpoch;

            string sizestr = uwr.GetResponseHeader("Content-Length");
            context.size = (sizestr != null) ? long.Parse(sizestr) : -1;

            // It's outdated if it's newer in the net, or the reported size differs.
            context.cacheHit = !((context.lastModified > locDT) || (context.size != locSize));
            return context;
        }

    }
    
    public static class WorldDownloader
    {
        public static (AsyncOperationExecutor<Context>, Context) PrepareDownloadWorld(string url, bool reload = false, int timeout = 600)
        {
            WorldDownloaderContext context = new()
            {
                url = url,
                reload = reload,
                targetfile = "world.zip"
            };


            AsyncOperationExecutor<Context> executor = new(new IAsyncOperation<Context>[]
            {
                new CheckCacheOp(),
                new DownloadOp(),
                new UnzipWorldFileOp()
            })
            {
                Timeout = timeout
            };

            return (executor, context);

        }

        public static void EnterDownloadedWorld(Context _context)
        {
            WorldDownloaderContext context = _context as WorldDownloaderContext;

            string worldABF = context.worldAssetBundleFile;

            Debug.Log($"Download complete, world={worldABF}");

            // Deploy the scene loader.
            GameObject go = new("_SceneLoader");
            go.AddComponent<Persistence>();
            SceneLoader sl = go.AddComponent<SceneLoader>();
            sl.OnFinishingSceneChange += WorldDownloaderLow.MoveToDownloadedWorld;
            sl.Name = worldABF;
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
