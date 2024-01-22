/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Ipfs;
using Arteranos.Services;

namespace Arteranos.Core.Operations
{
    internal class AssetDownloadOp : IAsyncOperation<Context>
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
            AssetDownloaderContext context = _context as AssetDownloaderContext;

            IDataBlock fi = await IPFSService.Ipfs.FileSystem.ListFileAsync(context.Cid, token);

            totalBytes = fi.Size;
            totalBytesMag = Utils.Magnitude(totalBytes);

            using Stream inStream = await IPFSService.Ipfs.FileSystem.ReadFileAsync(context.Cid, cancel: token);

            string dir = Path.GetDirectoryName(context.TargetFile);
            if(!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            using FileStream outStream = File.Create(context.TargetFile);

            await Utils.CopyWithProgress(inStream, outStream,
                bytes => {
                    actualBytes = bytes;
                    ProgressChanged((float)bytes / totalBytes);
                }, token);

            return context;
        }
    }
}
