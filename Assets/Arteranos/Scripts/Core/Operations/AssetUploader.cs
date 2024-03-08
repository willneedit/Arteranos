/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.IO;
using System.Threading.Tasks;

using Arteranos.Core;
using System.Threading;
using Utils = Arteranos.Core.Utils;
using Ipfs;
using Arteranos.Services;
using Ipfs.CoreApi;
using System.Net.Http;
using System.IO.Pipes;
using UnityEngine;

namespace Arteranos.Core.Operations
{
    internal class UploadDirectoryToIPFS : IAsyncOperation<Context>
    {
        public int Timeout { get; set; }
        public float Weight { get; set; } = 1.0f;
        public string Caption { get => GetProgressText(); }
        public Action<float> ProgressChanged { get; set; }

        private string GetProgressText()
        {
            return "Uploading...";
        }

        public async Task<Context> ExecuteAsync(Context _context, CancellationToken token)
        {
            AssetUploaderContext context = _context as AssetUploaderContext;

            string path = $"{context.TempFile}.dir";

            AddFileOptions ao = new()
            {
                Pin = context.pin
            };

            try
            {
                IFileSystemNode fsn = await IPFSService.AddDirectory(path, options: ao);
                context.Cid = fsn.Id;
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }

            return context;
        }
    }
    internal class UploadFileToIPFS : IAsyncOperation<Context>
    {
        public int Timeout { get; set; }
        public float Weight { get; set; } = 1.0f;
        public string Caption { get => GetProgressText(); }
        public Action<float> ProgressChanged { get; set; }

        private long actualBytes = 0;
        private long totalBytes = -1;
        private string totalBytesMag = null;

        private string GetProgressText()
        {
            if (totalBytesMag == null || totalBytes <= 0) return "Uploading...";

            return $"Uploading ({Utils.Magnitude(actualBytes)} of {totalBytesMag})...";
        }

        public async Task<Context> ExecuteAsync(Context _context, CancellationToken token)
        {
            AssetUploaderContext context = _context as AssetUploaderContext;

            AddFileOptions ao = new()
            {
                Pin = context.pin
            };

            try
            {
                FileInfo fileInfo = new(context.TempFile);
                totalBytes = fileInfo.Length;
                totalBytesMag = Utils.Magnitude(totalBytes);

                using Stream stream = File.OpenRead(context.TempFile);

                using AnonymousPipeServerStream pipeServer = new();
                using AnonymousPipeClientStream pipeClient = new(pipeServer.GetClientHandleAsString());

                _ = Utils.CopyWithProgress(stream, pipeServer, bytes => {
                    actualBytes = bytes;
                    ProgressChanged((float) bytes / totalBytes);
                }, token);
                IFileSystemNode fsn = await IPFSService.AddStream(pipeClient, "", ao, token);

                context.Cid = fsn.Id;
            }
            finally
            {
                if (File.Exists(context.TempFile)) File.Delete(context.TempFile);
            }

            return context;
        }
    }

    internal class DownloadFromWeb : IAsyncOperation<Context>
    {
        public int Timeout { get; set; }
        public float Weight { get; set; } = 1.0f;
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
            Stream inStream = null;

            AssetUploaderContext context = _context as AssetUploaderContext;

            string assetURL = context.AssetURL;

            // Strip quotes
            if (assetURL.StartsWith("\"") && assetURL.EndsWith("\""))
                assetURL = assetURL[1..^1];

            // Strip 'file:///' prefix
            if (assetURL.StartsWith("file:///"))
                assetURL = assetURL[8..];

            if (assetURL.StartsWith("http://") || assetURL.StartsWith("https://"))
            {
                // Deal with web resources
                using HttpClient client = new();
                client.Timeout = TimeSpan.FromSeconds(Timeout);
                using HttpResponseMessage response = await client.GetAsync(assetURL);
                response.EnsureSuccessStatusCode();
                totalBytes = response.Content.Headers.ContentLength ?? -1;
                inStream = await response.Content.ReadAsStreamAsync();
            }
            else if(assetURL.StartsWith("resource:///"))
            {
                assetURL = assetURL[12..];

                TextAsset ta = Resources.Load<TextAsset>(assetURL);
                inStream = new MemoryStream(ta.bytes);
                inStream.Position = 0;
            }
            else
            {
                // Deal with local (file) resources
                FileInfo fileInfo = new(assetURL);
                totalBytes = fileInfo.Length;
                inStream = File.OpenRead(assetURL);
            }
            totalBytesMag = Utils.Magnitude(totalBytes);


            using FileStream outStream = File.Create(context.TempFile);

            await Utils.CopyWithProgress(inStream, outStream, 
                bytes => {
                    actualBytes = bytes;
                    ProgressChanged((float) bytes / totalBytes);
                }, token);

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
                TempFile = $"{Path.GetTempFileName()}",
                pin = pin
            };

            AsyncOperationExecutor<Context> executor = new(new IAsyncOperation<Context>[]
            {
                new DownloadFromWeb(),
                new UploadFileToIPFS()
            })
            {
                Timeout = timeout
            };

            return (executor, context);
        }

        public static Cid GetUploadedCid(Context _context)
            => (_context as AssetUploaderContext).Cid;
    }
}
