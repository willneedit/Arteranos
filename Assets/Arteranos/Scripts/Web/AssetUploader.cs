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

namespace Arteranos.Web
{
    public class AssetUploaderContext : Context
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
                FileInfo fileInfo = new FileInfo(context.TempFile);
                totalBytes = fileInfo.Length;
                totalBytesMag = Utils.Magnitude(totalBytes);

                using Stream stream = File.OpenRead(context.TempFile);

                using AnonymousPipeServerStream pipeServer = new AnonymousPipeServerStream();
                using AnonymousPipeClientStream pipeClient =
                  new AnonymousPipeClientStream(pipeServer.GetClientHandleAsString());

                _ = Utils.CopyWithProgress(stream, pipeServer, bytes => {
                    actualBytes = bytes;
                    ProgressChanged((float) bytes / totalBytes);
                }, token);
                IFileSystemNode fsn = await IPFSService.Ipfs.FileSystem.AddAsync(pipeClient, "", ao, token);

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

            if(context.AssetURL.StartsWith("file://"))
            {
                string path = context.AssetURL[8..];
                FileInfo fileInfo = new FileInfo(path);
                totalBytes = fileInfo.Length;

                inStream = File.OpenRead(path);
            }
            else
            {
                using HttpClient client = new();

                client.Timeout = TimeSpan.FromSeconds(Timeout);

                using HttpResponseMessage response = await client.GetAsync(context.AssetURL);

                response.EnsureSuccessStatusCode();

                totalBytes = response.Content.Headers.ContentLength ?? -1;

                inStream = await response.Content.ReadAsStreamAsync();
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
                new UploadToIPFS()
            })
            {
                Timeout = timeout
            };

            return (executor, context);
        }

    }
}
