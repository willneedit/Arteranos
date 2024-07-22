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
using ICSharpCode.SharpZipLib.Tar;

namespace Arteranos.Core.Operations
{
    internal class AssetDownloadOp : IAsyncOperation<Context>
    {
        public int Timeout { get; set; }
        public float Weight { get; set; } = 8.0f;
        public string Caption { get => GetProgressText(); }
        public Action<float> ProgressChanged { get; set; }

        private long actualBytes = 0;
        private string totalBytesMag = null;

        private string GetProgressText()
        {
            if (totalBytesMag == null) return "Downloading...";

            return $"Downloading ({Utils.Magnitude(actualBytes)} of {totalBytesMag})...";
        }

        public async Task<Context> ExecuteAsync(Context _context, CancellationToken token)
        {
            AssetDownloaderContext context = _context as AssetDownloaderContext;

            IFileSystemNode fi = await G.IPFSService.ListFile(context.path, token);

            context.Size = fi.Size;
            totalBytesMag = Utils.Magnitude(context.Size);

            string dir = Path.GetDirectoryName(context.TargetFile);
            if(!Directory.Exists(dir)) Directory.CreateDirectory(dir);


            if(!context.isTarred)
            {
                // Read plain file
                byte[] contents = await G.IPFSService.ReadBinary(context.path, cancel: token);

                File.WriteAllBytes(context.TargetFile, contents);
            }
            else
            {
                // Extract directory
                Stream tar = await G.IPFSService.Get(context.path, token);
                using TarArchive archive = TarArchive.CreateInputTarArchive(tar);
                archive.ProgressMessageEvent += (a, e, m) =>
                {
                    actualBytes += e.Size;
                    ProgressChanged((float)actualBytes / context.Size);
                };
                archive.ExtractContents(context.TargetFile);
            }

            return context;
        }
    }
}
