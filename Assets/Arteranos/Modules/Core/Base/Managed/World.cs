/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.WorldEdit;
using Ipfs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;


namespace Arteranos.Core.Managed
{
    public interface IFavouriteable
    {
        void Favourite();
        void Unfavourite();
        bool IsFavourited {  get; }
        void UpdateLastSeen();
        DateTime LastSeen { get; }
    }

    public class World : IFavouriteable
    {
        public event Action<long, long> OnReportingProgress;

        public Cid RootCid { get; private set; } = null;

        private World() { }

        public World(Cid rootCid)
        {
            RootCid = rootCid;

            TemplateCid = new(async () => (await GetWorldLinks()).Item1);
            DecorationCid = new(async () => (await GetWorldLinks()).Item2);
            WorldInfo = new(async () => await GetWorldInfo());
            TemplateInfo = new(async () => await GetTemplateInfo());
            ScreenshotPNG = new(async () => await GetActiveScreenshot());

            TemplateContent = new(async () => await GetAssetBundle());
            DecorationDontent = new(async () => await GetWorldDecoration());
        }

        public static implicit operator World(Cid rootCid) => new(rootCid);

        /// <summary>
        /// The underlying world template
        /// </summary>
        public readonly AsyncLazy<Cid> TemplateCid;

        /// <summary>
        /// The decoration, null if there idn't one (aka blank world)
        /// </summary>
        public readonly AsyncLazy<Cid> DecorationCid;

        /// <summary>
        /// The active World Info, same as TemplateInfo if it's a blank world
        /// </summary>
        public readonly AsyncLazy<WorldInfoNetwork> WorldInfo;

        /// <summary>
        /// The template's info.
        /// </summary>
        public readonly AsyncLazy<WorldInfoNetwork> TemplateInfo;
        /// <summary>
        /// The screemshot PNG
        /// </summary>
        public readonly AsyncLazy<byte[]> ScreenshotPNG;

        /// <summary>
        /// The template's asset bundle
        /// </summary>
        public readonly AsyncLazy<AssetBundle> TemplateContent;

        /// <summary>
        /// The world decoration's content
        /// </summary>
        public readonly AsyncLazy<IWorldDecoration> DecorationDontent;

        public async Task<bool> IsFullWorld() => await DecorationCid != null;


        private string m_TemplateCid = null;
        private string m_DecorationCid = null;

        private async Task<(string,string)> GetWorldLinks()
        {
            if (m_TemplateCid != null) return (m_TemplateCid, m_DecorationCid);

            Dictionary<string, IFileSystemLink> dir = new();

            IFileSystemNode fsn = await G.IPFSService.ListFile(RootCid);
            if (!fsn.IsDirectory)
                throw new InvalidDataException($"{RootCid} is not a directory");
            foreach (IFileSystemLink file in fsn.Links)
                dir.Add(file.Name, file);

            m_TemplateCid = dir.ContainsKey("Template")
                ? dir["Template"].Id
                : RootCid;

            m_DecorationCid = dir.ContainsKey("Decoration")
                ? dir["Decoration"].Id
                : null;

            return (m_TemplateCid, m_DecorationCid);
        }

        private async Task<byte[]> GetActiveScreenshot()
        {           
            using CancellationTokenSource cts = new(4000);
            return await G.IPFSService.ReadBinary($"{RootCid}/Screenshot.png", cancel: cts.Token);
        }

        private async Task<WorldInfoNetwork> GetTemplateInfo()
        {
            string targeted = await TemplateCid;

            using CancellationTokenSource cts = new(4000);
            byte[] data = await G.IPFSService.ReadBinary($"{targeted}/Metadata.json", cancel: cts.Token);
            string json = Encoding.UTF8.GetString( data );

            WorldMetaData metaData = WorldMetaData.Deserialize(json);

            WorldInfoNetwork win = new()
            {
                WorldCid = (Cid) TemplateCid,
                WorldName = metaData.WorldName,
                WorldDescription = metaData.WorldDescription,
                Author = metaData.AuthorID,
                ContentRating = metaData.ContentRating,
                Signature = null,
                ScreenshotPNG = null,
                Created = metaData.Created,
            };
            return win;
        }

        private async Task<WorldInfoNetwork> GetWorldInfo()
        {
            WorldInfoNetwork win = await IsFullWorld() 
                ? (await GetWorldDecoration()).Info 
                : await GetTemplateInfo();

            // Add the self reference
            win.WorldCid = RootCid;
            return win;
        }

        private async Task<IWorldDecoration> GetWorldDecoration()
        {
            using CancellationTokenSource cts = new(4000);
            using MemoryStream ms = await G.IPFSService.ReadIntoMS(await DecorationCid, cancel: cts.Token);
            return G.WorldEditorData.DeserializeWD(ms);
        }

        // TODO - Move into common space to make it accessible to kit loading
        private static async Task<AssetBundle> LoadAssetBundle(string path, Action<long, long> reportProgress = null, CancellationToken cancel = default)
        {
            AssetBundle resultAB = null;
            SemaphoreSlim waiter = new(0, 1);

            IEnumerator Cor()
            {
                AssetBundle manifestAB = null;
                yield return AssetBundle.LoadFromIPFS($"{path}/{Utils.GetArchitectureDirName()}/{Utils.GetArchitectureDirName()}", _result => manifestAB = _result, cancel: cancel);

                if (manifestAB != null)
                {
                    AssetBundleManifest manifest = ((UnityEngine.AssetBundle)manifestAB).LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                    string actualABName = manifest.GetAllAssetBundles()[0];

                    yield return AssetBundle.LoadFromIPFS($"{path}/{Utils.GetArchitectureDirName()}/{actualABName}", _result => resultAB = _result, reportProgress, cancel);

                    manifestAB.Dispose();
                }

                waiter.Release();
            }

            TaskScheduler.ScheduleCoroutine(Cor);

            await waiter.WaitAsync();

            return resultAB;
        }

        private async Task<AssetBundle> GetAssetBundle()
        {
            // TODO ten minutes timeout? Configurable?
            using CancellationTokenSource cts = new(600000);
            return await LoadAssetBundle(await TemplateCid, (bytes, total) => OnReportingProgress?.Invoke(bytes, total), cts.Token);
        }

        // ---------------------------------------------------------------

        public DateTime LastSeen => DateTime.MinValue;

        public bool IsFavourited => G.Client.FavouritedWorlds.Contains(RootCid);

        public void Favourite()
        {
            Client cs = G.Client;
            if (!cs.FavouritedWorlds.Contains(RootCid))
                cs.FavouritedWorlds.Add(RootCid);

            cs.Save();
        }

        public void Unfavourite()
        {
            Client cs = G.Client;
            cs.FavouritedWorlds.Remove(RootCid);

            cs.Save();
        }

        public void UpdateLastSeen()
        {
            // TODO Implement
        }

        public static List<Cid> ListFavourites()
        {
            return G.Client.FavouritedWorlds;
        }
    }
}