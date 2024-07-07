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
using System.Collections.Generic;
using System.Text;
using Arteranos.WorldEdit;

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

            var (templateCid, decorationCid) = await OpUtils.GetWorldLinks(context.WorldCid, token);
            context.TemplateInfo = await WorldDownloader.ExtractTemplateInfo(templateCid, token);
            context.TemplateCid = templateCid;

            if (decorationCid == null)
                context.WorldInfo = context.TemplateInfo;
            else
            {
                WorldDecoration wd = await WorldDownloader.ExtractDecoration(decorationCid, token);
                context.WorldInfo = new()
                {
                    win = wd.Info,
                    Updated = DateTime.MinValue
                };
            }

            context.WorldInfo.DBUpdate();

            return context;
        }
    }

    internal class DownloadTemplateOp : IAsyncOperation<Context>
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

            WorldDownloadContext context = _context as WorldDownloadContext;

            // Invalidate the 'current' asset bundle path.
            WorldDownloader.CurrentWorldAssetBundlePath = null;

            Cid worldCid = context.WorldCid;
            var (templateCid, decorationCid) = await OpUtils.GetWorldLinks(worldCid, token);
            context.TemplateCid = templateCid;

            if(decorationCid != null)
                context.Decoration = await WorldDownloader.ExtractDecoration(decorationCid, token);

            IFileSystemLink found = await OpUtils.ExtractAssetArchive(context.TemplateCid, token);

            totalBytes = found.Size;
            totalBytesMag = Utils.Magnitude(totalBytes);
            Cid assetBundleCid = found.Id;

            // Clean out the unpacked files - IPFS takes care of the world data with its
            // sense of importance (pinned/unpinned like favourited/unfavourited)
            if (Directory.Exists(Utils.WorldCacheRootDir)) Directory.Delete(Utils.WorldCacheRootDir, true);
            Directory.CreateDirectory(Utils.WorldCacheRootDir);
            context.WorldAssetBundlePath = $"{Utils.WorldCacheRootDir}/{assetBundleCid}";

            using Stream instr = await IPFSService.ReadFile(assetBundleCid, token);
            using Stream outstr = File.Create(context.WorldAssetBundlePath);

            await Utils.CopyWithProgress(instr, outstr, _actual =>
            {
                actualBytes = _actual;
                ProgressChanged((float)_actual / totalBytes);
            });

            WorldDownloader.CurrentWorldAssetBundlePath = context.WorldAssetBundlePath;

            // As an afterthought, pin the world in the local IPFS node.
            try
            {
                await IPFSService.PinCid(worldCid, true);
            }
            catch { }

            return context;
        }
    }
 
    public static class WorldDownloader
    {
        internal static async Task<WorldDecoration> ExtractDecoration(Cid decoration, CancellationToken cancel)
        {
            byte[] decorationData = await IPFSService.ReadBinary(decoration, cancel: cancel);
            using MemoryStream ms = new(decorationData);
            return WorldEditorData.Instance.DeserializeWD(ms);
        }

        internal static async Task<WorldInfo> ExtractTemplateInfo(Cid templateCid, CancellationToken cancel)
        {
            IFileSystemNode fsn = await IPFSService.ListFile(templateCid, cancel);
            IEnumerable<IFileSystemLink> links = fsn.Links;

            string screenshotName = null;
            long screenshotSize = 0;

            foreach (IFileSystemLink link in links)
                if (link.Name.StartsWith("Screenshot"))
                {
                    screenshotName = link.Name;
                    screenshotSize = link.Size;
                    break;
                }

            byte[] screenshotBytes = await IPFSService.ReadBinary($"{templateCid}/{screenshotName}", cancel: cancel);

            byte[] mdbytes = await IPFSService.ReadBinary($"{templateCid}/Metadata.json", cancel: cancel);

            string json = Encoding.UTF8.GetString(mdbytes);
            WorldMetaData metaData = WorldMetaData.Deserialize(json);

            WorldInfo wi = new()
            {
                win = new()
                {
                    WorldCid = templateCid,
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
            return wi;
        }

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

        public static (AsyncOperationExecutor<Context>, Context) PrepareGetWorldTemplate(Cid WorldCid, int timeout = 600)
        {
            WorldDownloadContext context = new()
            {
                WorldCid = WorldCid
            };

            AsyncOperationExecutor<Context> executor = new(new IAsyncOperation<Context>[]
            {
                // TODO #115: Before: Check type: pure template or decorated world data (template + object list)
                new DownloadTemplateOp(),
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

        public static WorldDecoration GetWorldDecoration(Context _context)
            => (_context as WorldDownloadContext).Decoration;
    }
}
