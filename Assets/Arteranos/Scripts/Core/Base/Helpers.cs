/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using Arteranos.Core.Cryptography;
using Ipfs;
using Ipfs.Cryptography.Proto;
using Ipfs.CoreApi;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Ipfs.Http;
using Arteranos.WorldEdit;

using AsyncOperation = UnityEngine.AsyncOperation;

namespace Arteranos.Services
{
    // -------------------------------------------------------------------
    #region Services helpers

    public abstract class IPFSService : MonoBehaviour
    {
        public abstract IpfsClientEx Ipfs_ { get; }
        public abstract Peer Self_ { get; }
        public abstract SignKey ServerKeyPair_ { get; }
        public abstract Cid IdentifyCid_ { get; protected set; }
        public abstract Cid CurrentSDCid_ { get; protected set; }
        public abstract bool UsingPubsub_ { get; protected set; }


        public abstract Task<IPAddress> GetPeerIPAddress_(MultiHash PeerID, CancellationToken token = default);
        public abstract Task FlipServerDescription_(bool reload);
        public abstract Task PinCid_(Cid cid, bool pinned, CancellationToken token = default);
        public abstract Task<byte[]> ReadBinary_(string path, Action<long> reportProgress = null, CancellationToken cancel = default);
        public abstract void DownloadServerOnlineData_(MultiHash SenderPeerID, Action callback = null);
        public static IPFSService Instance { get; protected set; }

        public static Peer Self 
            => Instance.Self_;
        public static SignKey ServerKeyPair
            => Instance.ServerKeyPair_;
        public static PublicKey ServerPublicKey
            => ServerKeyPair.PublicKey;
        public static Cid IdentifyCid
            => Instance.IdentifyCid_;
        public static Cid CurrentSDCid
            => Instance.CurrentSDCid_;
        public static bool UsingPubsub
            => Instance.UsingPubsub_;
        public static async Task<IPAddress> GetPeerIPAddress(MultiHash PeerID, CancellationToken token = default)
            => await Instance.GetPeerIPAddress_(PeerID, token).ConfigureAwait(false);
        public static async Task FlipServerDescription(bool reload)
            => await Instance.FlipServerDescription_(reload).ConfigureAwait(false);
        public static async Task PinCid(Cid cid, bool pinned, CancellationToken cancel = default)
            => await Instance.PinCid_(cid, pinned, cancel).ConfigureAwait(false);
        public static async Task<IEnumerable<Cid>> ListPinned(CancellationToken cancel = default)
            => await Instance.Ipfs_.Pin.ListAsync(cancel).ConfigureAwait(false);
        public static async Task<Stream> ReadFile(string path, CancellationToken cancel = default)
            => await Instance.Ipfs_.FileSystem.ReadFileAsync(path, cancel).ConfigureAwait(false);
        public static async Task<byte[]> ReadBinary(string path, Action<long> reportProgress = null, CancellationToken cancel = default)
            => await Instance.ReadBinary_(path, reportProgress, cancel).ConfigureAwait(false);
        public static void DownloadServerOnlineData(MultiHash SenderPeerID, Action callback = null)
            => Instance.DownloadServerOnlineData_(SenderPeerID, callback);

        public static async Task<Stream> Get(string path, CancellationToken cancel = default)
            => await Instance.Ipfs_.FileSystem.GetAsync(path, cancel: cancel).ConfigureAwait(false);
        public static async Task<IFileSystemNode> AddStream(Stream stream, string name = "", AddFileOptions options = null, CancellationToken cancel = default)
            => await Instance.Ipfs_.FileSystem.AddAsync(stream, name, options, cancel).ConfigureAwait(false);
        public static async Task<string> ResolveAsync(string path, bool recursive = true, CancellationToken cancel = default)
            => await Instance.Ipfs_.ResolveAsync(path, recursive, cancel).ConfigureAwait(false);
        public static async Task<IFileSystemNode> ListFile(string path, CancellationToken cancel = default)
            => await Instance.Ipfs_.FileSystem.ListAsync(path, cancel).ConfigureAwait(false);
        public static async Task<IFileSystemNode> AddDirectory(string path, bool recursive = true, AddFileOptions options = null, CancellationToken cancel = default)
            => await Instance.Ipfs_.FileSystem.AddDirectoryAsync(path, recursive, options, cancel).ConfigureAwait(false);
        public static async Task RemoveGarbage(CancellationToken cancel = default)
            => await Instance.Ipfs_.BlockRepository.RemoveGarbageAsync(cancel).ConfigureAwait(false);

        public static async Task<Cid> ResolveToCid(string path, CancellationToken cancel = default)
        {
            string resolved = await Instance.Ipfs_.ResolveAsync(path, cancel: cancel).ConfigureAwait(false);
            if (resolved == null || resolved.Length < 6 || resolved[0..6] != "/ipfs/") return null;
            return resolved[6..];
        }
    }

    public abstract class TransitionProgressStatic : MonoBehaviour
    {
        public static TransitionProgressStatic Instance;

        public abstract void OnProgressChanged(float progress, string progressText);

        public static IEnumerator TransitionFrom()
        {
            G.XRVisualConfigurator.StartFading(1.0f);
            yield return new WaitForSeconds(0.5f);

            AsyncOperation ao = SceneManager.LoadSceneAsync("Transition");
            yield return new WaitUntil(() => ao.isDone);


            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            G.XRControl.MoveRig();

            G.XRVisualConfigurator.StartFading(0.0f);

            yield return new WaitUntil(() => Instance);

            G.SysMenu.EnableHUD(false);
        }

        // NOTE: Needs preloaded world! Just deploys the sceneloader which it uses
        // the current preloaded world asset bundle!
        public static IEnumerator TransitionTo(Cid WorldCid, string WorldName)
        {
            G.XRVisualConfigurator.StartFading(1.0f);
            yield return new WaitForSeconds(0.5f);

            yield return MoveToPreloadedWorld(WorldCid, WorldName);

            G.XRVisualConfigurator.StartFading(0.0f);

            G.SysMenu.EnableHUD(true);
        }

        private static IEnumerator MoveToPreloadedWorld(Cid WorldCid, string WorldName)
        {
            if (WorldCid == null)
            {
                AsyncOperation ao = SceneManager.LoadSceneAsync("OfflineScene");
                while (!ao.isDone) yield return null;

                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();

                G.XRControl.MoveRig();
            }
            else
                yield return EnterDownloadedWorld();

            SettingsManager.WorldCid = WorldCid;
            SettingsManager.WorldName = WorldName;
        }

        public static IEnumerator EnterDownloadedWorld()
        {
            string worldABF = Core.Operations.WorldDownloader.CurrentWorldAssetBundlePath;
            IWorldDecoration worldDecoration = Core.Operations.WorldDownloader.CurrentWorldDecoration;

            Debug.Log($"Download complete, world={worldABF}");

            yield return G.SceneLoader.LoadScene(worldABF);

            if (worldDecoration != null)
            {
                Debug.Log("World Decoration detected, building hand-edited world");
                yield return G.WorldEditorData.BuildWorld(worldDecoration);
            }
            else
                Debug.Log("World is a bare template");

            G.XRControl.MoveRig();
        }
    }

    #endregion
    // -------------------------------------------------------------------
}
