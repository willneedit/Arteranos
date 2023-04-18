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
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

using Arteranos.Core;
using System.Threading;

namespace Arteranos.Web
{
    internal class WorldDownloaderContext : Context
    {
        public string url = null;
        public string targetfile = null;
        public bool reload = false;
        public string cachedir = null;
        public bool cacheHit = false;
        public string worldZipFile = null;
        public string worldAssetBundleFile = null;
    }

    internal class UnzipWorldFileOp : IAsyncOperation<WorldDownloaderContext>
    {
        public int Timeout { get; set; }
        public float Weight { get; set; } = 2.0f;
        public string Caption { get; set; } = "Uncompressing file";
        public Action<float> ProgressChanged { get; set; }

        public async Task<WorldDownloaderContext> ExecuteAsync(WorldDownloaderContext context, CancellationToken token)
        {
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

    internal class DownloadOp : IAsyncOperation<WorldDownloaderContext>
    {
        public int Timeout { get; set; }
        public float Weight { get; set; } = 8.0f;
        public string Caption { 
            get => "Downloading...";
            set { }
        }

        public Action<float> ProgressChanged { get; set; }

        public async Task<WorldDownloaderContext> ExecuteAsync(WorldDownloaderContext context, CancellationToken token)
        {
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

            if(uwr.result == UnityWebRequest.Result.ProtocolError ||
                uwr.result == UnityWebRequest.Result.ConnectionError)
                throw new FileNotFoundException(uwr.error, context.worldZipFile);

            return context;
        }
    }

    internal class CheckCacheOp : IAsyncOperation<WorldDownloaderContext>
    {
        public int Timeout { get; set; }
        public float Weight { get; set; } = 1.0f;
        public string Caption
        {
            get => "Connecting";
            set { }
        }

        public Action<float> ProgressChanged { get; set; }

        public async Task<WorldDownloaderContext> ExecuteAsync(WorldDownloaderContext context, CancellationToken token)
        {
            Hash128 hash = new();
            byte[] bytes = Encoding.UTF8.GetBytes(context.url);
            hash.Append(bytes);
            string hashstr = hash.ToString();

            context.cachedir = $"{Application.temporaryCachePath}/WorldCache/{hashstr[0..2]}/{hashstr[2..]}";
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

            DateTime netDT = DateTime.UnixEpoch;
            long netSize = -1;

            Dictionary<string, string> responseHeader = uwr.GetResponseHeaders();

            if(responseHeader.TryGetValue("Last-Modified", out string lmstr))
                netDT = DateTime.Parse(lmstr);

            if(responseHeader.TryGetValue("Content-Length", out string sizestr))
                netSize = long.Parse(sizestr);

            // It's outdated if it's newer in the net, or the reported size differs.
            bool outdated = (netDT > locDT) || (netSize != locSize);

            context.cacheHit = !outdated;
            return context;
        }
    }

    
    public class WorldDownloader : MonoBehaviour
    {

        private async void DownloadWorldAsync(string url, bool reload = false, int timeout = 600)
        {
            WorldDownloaderContext context = new()
            {
                url = url,
                reload = reload,
                targetfile = "world.zip"
            };


            AsyncOperationExecutor<WorldDownloaderContext> executor = new(new IAsyncOperation<WorldDownloaderContext>[]
            {
                new CheckCacheOp(),
                new DownloadOp(),
                new UnzipWorldFileOp()
            });

            executor.Timeout = timeout;

            //executor.ProgressChanged +=
            //    (progress, caption) => Debug.Log($"Progress: {caption}, {progress * 100.0f}%");

            executor.Completed += (context) =>
            {
                Debug.Log($"Completed, file {context.worldAssetBundleFile}");
            };

            try
            {
                context = await executor.ExecuteAsync(context);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Caught exception...");
                Debug.LogException(ex);
            }
        }
        private void OnEnable()
        {
            DownloadWorldAsync(
                "https://github.com/willneedit/willneedit.github.io/blob/master/Abbey.zip?raw=true"
                // "file://C:/Users/willneedit/Desktop/world.zip"
                );

            Debug.Log("Spawned the downloader in the background, we'll see...");
        }
    }
}
