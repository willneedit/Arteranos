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
using UnityEngine.Networking;

using Arteranos.Core;
using System.Threading;
using Utils = Arteranos.Core.Utils;
using Ipfs;
using Arteranos.Services;
using Ipfs.CoreApi;

namespace Arteranos.Web
{
    internal class AssetUploaderContext : Context
    {
        public string AssetURL = null;
        public string TempFile = null;
        public bool pin = false;
        public Cid Cid = null;
    }

    internal class UploadToIPFS : IAsyncOperation<Context>
    {
        public int Timeout { get; set; }
        public float Weight { get; set; } = 1.0f;
        public string Caption => "Uploading to the IPFS...";
        public Action<float> ProgressChanged { get; set; }

        // TransferProgress lastProgress = null;
        public async Task<Context> ExecuteAsync(Context _context, CancellationToken token)
        {
            // TODO Upload progress indicator
            AssetUploaderContext context = _context as AssetUploaderContext;

            AddFileOptions ao = new()
            {
                // Progress = new Progress<TransferProgress>(t => lastProgress = t),
                Pin = context.pin
            };

            IFileSystemNode fsn = await IPFSService.Ipfs.FileSystem.AddFileAsync(context.TempFile, ao);
            context.Cid = fsn.Id;

            if(File.Exists(context.TempFile)) File.Delete(context.TempFile);

            return context;
        }
    }

    internal class DownloadFromWeb : IAsyncOperation<Context>
    {
        public int Timeout { get; set; }
        public float Weight { get; set; } = 1.0f;
        public string Caption { get => GetProgressText(); }
        public Action<float> ProgressChanged { get; set; }

        private float normalizedProgress = 0.0f;
        private long totalBytes = -1;
        private string totalBytesMag = null;

        private string GetProgressText()
        {
            long actualBytes = (long)(normalizedProgress * totalBytes);

            // Maybe the UI buildup was quicker than the initialization...
            if (totalBytesMag == null || totalBytes <= 0) return "Downloading...";

            return $"Downloading ({Utils.Magnitude(actualBytes)} of {totalBytesMag})...";
        }

        public async Task<Context> ExecuteAsync(Context _context, CancellationToken token)
        {
            AssetUploaderContext context = _context as AssetUploaderContext;

            using UnityWebRequest uwr = new(context.AssetURL, UnityWebRequest.kHttpVerbGET);

            uwr.timeout = Timeout;
            uwr.downloadHandler = new DownloadHandlerFile(context.TempFile);

            UnityWebRequestAsyncOperation uwr_ao = uwr.SendWebRequest();

            while (!uwr_ao.isDone && !token.IsCancellationRequested)
            {
                if (totalBytes < 0)
                {
                    string size = uwr.GetResponseHeader("Content-Length");
                    if (size != null)
                    {
                        totalBytes = Convert.ToInt64(size);
                        totalBytesMag = Utils.Magnitude(totalBytes);
                    }
                }

                await Task.Yield();
                normalizedProgress = uwr.downloadProgress;
                ProgressChanged?.Invoke(uwr.downloadProgress);
            }

            if (uwr.result == UnityWebRequest.Result.ProtocolError || uwr.result == UnityWebRequest.Result.ConnectionError)
                throw new FileNotFoundException(uwr.error, context.TempFile);

            return context;
        }
    }

    public static class AssetUploader
    {
        public static (AsyncOperationExecutor<Context>, Context) PrepareUploadToIPFS(string assetURL, int timeout = 600, bool pin = false)
        {
            AssetUploaderContext context = new()
            {
                AssetURL = assetURL,
                TempFile = $"{Application.temporaryCachePath}/{Path.GetTempFileName()}",
                pin = pin
            };

            AsyncOperationExecutor<Context> executor = new(new IAsyncOperation<Context>[]
            {
                new DownloadFromWeb(),
                new UploadToIPFS()
            })
            {
                Timeout = timeout
            };

            return (executor, context);
        }

    }
}
