/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Ipfs;
using Ipfs.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;


namespace Arteranos.Core.Managed
{
    /// <summary>
    /// Managed version of AssetBundle, to let it only live as long as the
    /// reference is held.
    /// </summary>
    public class AssetBundle : IDisposable
    {
        private bool disposed = false;
        private UnityEngine.AssetBundle assetBundle = null;

        public static implicit operator UnityEngine.AssetBundle(AssetBundle assetBundle)
        {
            if (assetBundle.disposed)
                throw new ObjectDisposedException(nameof(assetBundle));

            return assetBundle.assetBundle;
        }

        public static implicit operator AssetBundle(UnityEngine.AssetBundle assetBundle)
            => new() { assetBundle = assetBundle };

        public static IEnumerator LoadFromIPFS(string path, Action<AssetBundle> result, Action<long> reportProgress = null, CancellationToken cancel = default)
        {
            MemoryStream ms = null;
            yield return Asyncs.Async2Coroutine(G.IPFSService.ReadIntoMS(path, reportProgress, cancel), _result => ms = _result);

            AssetBundleCreateRequest abc = UnityEngine.AssetBundle.LoadFromStreamAsync(ms);

            yield return new WaitUntil(() => abc.isDone);

            AssetBundle ab = abc.assetBundle;

            result.Invoke(ab);
        }

        public static async Task<AssetBundle> LoadFromIPFSAsync(string path, Action<long, long> reportProgress = null, CancellationToken cancel = default)
        {
            Cid cid = await G.IPFSService.ResolveToCid(path, cancel).ConfigureAwait(false);

            IFileSystemNode fsn = await G.IPFSService.ListFile(cid, cancel).ConfigureAwait(false);

            // No 'stat' implementation, just add the block sizes
            long totalBytes = 0;
            foreach (IFileSystemLink link in fsn.Links)
                totalBytes += link.Size;

            Action<long> rp = reportProgress != null ? (b) => reportProgress(b, totalBytes) : null;

            using MemoryStream ms = await G.IPFSService.ReadIntoMS(cid, rp).ConfigureAwait(false);

            AssetBundle ab = null;
            IEnumerator Cor()
            {
                AssetBundleCreateRequest abc = UnityEngine.AssetBundle.LoadFromStreamAsync(ms);
                yield return new WaitUntil(() => abc.isDone);

                ab = abc.assetBundle;
            }

            // UnityEngine functions need to be called from the main thread - not from worker thread
            TaskScheduler.ScheduleCoroutine(() => Cor());

            while(ab == null)
                await Task.Delay(100);

            return ab;

        }
        ~AssetBundle() { Dispose(); }

        public void Detach() => assetBundle = null;

        public void Dispose()
        {
            if(disposed) return;
            disposed = true;

            if(assetBundle != null) assetBundle.Unload(true);
            assetBundle = null;
        }
    }
}