/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Audio;
using Arteranos.Avatar;
using Arteranos.Core;
using Arteranos.Core.Cryptography;
using Arteranos.UI;
using Ipfs;
using Ipfs.Cryptography.Proto;
using Ipfs.CoreApi;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Ipfs.Http;
using Arteranos.WorldEdit;

using AsyncOperation = UnityEngine.AsyncOperation;

namespace Arteranos.Services
{
    // -------------------------------------------------------------------
    #region Services helpers
    public abstract class AudioManager : MonoBehaviour
    {
        protected abstract AudioMixerGroup MixerGroupEnv_ { get; }
        protected abstract AudioMixerGroup MixerGroupVoice_ { get; }
        protected abstract float VolumeEnv_ { get; set; }
        protected abstract float VolumeMaster_ { get; set; }
        protected abstract float VolumeVoice_ { get; set; }
        protected abstract float MicGain_ { get; set; }
        protected abstract int MicAGCLevel_ { get; set; }
        protected abstract IVoiceInput MicInput_ { get; set; }
        protected abstract int ChannelCount_ { get; }
        protected abstract int SampleRate_ { get; }
        protected abstract event Action<float[]> OnSampleReady_;
        protected abstract event Action<int, byte[]> OnSegmentReady_;
        protected abstract int? GetDeviceId_();
        protected abstract IVoiceOutput GetVoiceOutput_(int SampleRate, int ChannelCount);
        protected abstract void PushVolumeSettings_();
        protected abstract void RenewMic_();

        public static AudioManager Instance { get; set; }

        public static AudioMixerGroup MixerGroupEnv { get => Instance.MixerGroupEnv_; }
        public static AudioMixerGroup MixerGroupVoice { get => Instance.MixerGroupVoice_; }
        public static float VolumeEnv
        {
            get => Instance.VolumeEnv_;
            set => Instance.VolumeEnv_ = value;
        }
        public static float VolumeMaster
        {
            get => Instance.VolumeMaster_;
            set => Instance.VolumeMaster_ = value;
        }
        public static float VolumeVoice
        {
            get => Instance.VolumeVoice_;
            set => Instance.VolumeVoice_ = value;
        }
        public static float MicGain
        {
            get => Instance.MicGain_;
            set => Instance.MicGain_ = value;
        }
        public static int MicAGCLevel
        {
            get => Instance.MicAGCLevel_;
            set => Instance.MicAGCLevel_ = value;
        }

        public static int ChannelCount { get => Instance.ChannelCount_; }
        public static int SampleRate { get => Instance.SampleRate_; }

        public static event Action<float[]> OnSampleReady
        {
            add => Instance.OnSampleReady_ += value;
            remove { if (Instance != null) Instance.OnSampleReady_ -= value; }
        }

        public static event Action<int, byte[]> OnSegmentReady
        {
            add => Instance.OnSegmentReady_ += value;
            remove { if (Instance != null) Instance.OnSegmentReady_ -= value; }
        }


        public static int? GetDeviceId() => Instance.GetDeviceId_();
        public static IVoiceOutput GetVoiceOutput(int SampleRate, int ChannelCount)
            => Instance.GetVoiceOutput_(SampleRate, ChannelCount);

        public static void PushVolumeSettings() => Instance.PushVolumeSettings_();
        public static void RenewMic() => Instance.RenewMic_();

    }

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

            SysMenuStatic.EnableHUD(false);
        }

        // NOTE: Needs preloaded world! Just deploys the sceneloader which it uses
        // the current preloaded world asset bundle!
        public static IEnumerator TransitionTo(Cid WorldCid, string WorldName)
        {
            G.XRVisualConfigurator.StartFading(1.0f);
            yield return new WaitForSeconds(0.5f);

            yield return MoveToPreloadedWorld(WorldCid, WorldName);

            G.XRVisualConfigurator.StartFading(0.0f);

            SysMenuStatic.EnableHUD(true);
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

namespace Arteranos.Web
{
    // -------------------------------------------------------------------
    #region Web helpers
    public abstract class ConnectionManager : MonoBehaviour
    {
        protected abstract IEnumerator ConnectToServer_(MultiHash PeerID, Action<bool> callback);
        protected abstract void DeliverDisconnectReason_(string reason);
        protected abstract void ExpectConnectionResponse_();

        public static ConnectionManager Instance { get; protected set; }

        public static IEnumerator ConnectToServer(MultiHash PeerID, Action<bool> callback) 
            => Instance.ConnectToServer_(PeerID, callback);
        public static void DeliverDisconnectReason(string reason) 
            => Instance.DeliverDisconnectReason_(reason);
        public static void ExpectConnectionResponse()
            => Instance.ExpectConnectionResponse_();
    }
    #endregion
    // -------------------------------------------------------------------
}

namespace Arteranos.UI
{
    // -------------------------------------------------------------------
    #region UI factories

    public class AgreementDialogUIFactory : UIBehaviour
    {
        private static bool CanSkipAgreement(ServerInfo si)
        {
            Client client = SettingsManager.Client;
            byte[] serverTOSHash = si.PrivacyTOSNoticeHash;
            if (serverTOSHash == null) return true;

            if(client.ServerPasses.TryGetValue(si.SPKDBKey, out ServerPass sp))
            {
                // Server's [custom] agreement is unchanged.
                if(sp.PrivacyTOSHash != null && serverTOSHash.SequenceEqual(sp.PrivacyTOSHash)) return true;
            }

            // Server uses an unknown TOS deviating from the standard TOS, needs to ask.
            if (si.UsesCustomTOS) return false;

            byte[] currentTOSHash = Hashes.SHA256(SettingsManager.DefaultTOStext);

            // Only if the user's knowledge of the default TOS is up to date.
            return client.KnowsDefaultTOS != null && currentTOSHash.SequenceEqual(client.KnowsDefaultTOS);

        }


        public static IAgreementDialogUI New(ServerInfo serverInfo, Action disagree, Action agree)
        {
            string text = serverInfo?.PrivacyTOSNotice;

            // Can we skip it? Pleeeease..?
            if (text == null || CanSkipAgreement(serverInfo))
            {
                Client.UpdateServerPass(serverInfo, true, null);
                agree?.Invoke();
                return null;
            }

            GameObject go = Instantiate(BP.I.UI.AgreementDialog);
            IAgreementDialogUI AgreementDialogUI = go.GetComponent<IAgreementDialogUI>();
            AgreementDialogUI.OnDisagree += disagree;
            AgreementDialogUI.OnAgree += agree;
            AgreementDialogUI.ServerInfo = serverInfo;
            return AgreementDialogUI;
        }
    }

    public class DialogUIFactory : UIBehaviour
    {
        public static IDialogUI New()
        {
            GameObject go = Instantiate(BP.I.UI.Dialog);
            return go.GetComponent<IDialogUI>();
        }

    }

    public class ProgressUIFactory : UIBehaviour
    {
        public static IProgressUI New()
        {
            GameObject go = Instantiate(BP.I.UI.Progress);
            return go.GetComponent<IProgressUI>();
        }
    }

    public class AvatarGalleryUIFactory : UIBehaviour
    {
        public static IAvatarGalleryUI New()
        {
            Transform t = G.XRControl.rigTransform;
            Vector3 position = t.position;
            Quaternion rotation= t.rotation;
            position += rotation * Vector3.forward * 2f;

            GameObject go = Instantiate(
                BP.I.InApp.AvatarGalleryPedestal,
                position,
                rotation
                );
            return go.GetComponent<IAvatarGalleryUI>();
        }
    }

    public class NameplateUIFactory : UIBehaviour
    {
        public static INameplateUI New(GameObject bearer)
        {
            GameObject go;
            INameplateUI nameplate;

            nameplate = bearer.GetComponentInChildren<INameplateUI>(true);
            go = nameplate?.gameObject;

            if(nameplate == null)
            {
                GameObject original = BP.I.InApp.Nameplate;
                original.SetActive(false);

                go = Instantiate(original, bearer.transform);

                nameplate = go.GetComponent<INameplateUI>();
            }

            nameplate.Bearer = bearer.GetComponent<IAvatarBrain>();
            go.SetActive(true);

            return nameplate;
        }
    }

    public class TextMessageUIFactory : UIBehaviour
    {
        public static ITextMessageUI New(IAvatarBrain receiver)
        {
            GameObject go = Instantiate(BP.I.UI.TextMessage);
            ITextMessageUI textMessageUI = go.GetComponent<ITextMessageUI>();
            textMessageUI.Receiver = receiver;
            return textMessageUI;
        }
    }

    public class KickBanUIFactory : UIBehaviour
    {
        public static IKickBanUI New(IAvatarBrain target)
        {
            GameObject go = Instantiate(BP.I.UI.KickBan);
            IKickBanUI kickBanUI = go.GetComponentInChildren<IKickBanUI>();
            kickBanUI.Target = target;
            return kickBanUI;
        }
    }

    public abstract class SysMenuStatic : MonoBehaviour
    {
        public enum MenuKind
        {
            System,
            WorldEdit
        }

        public static SysMenuStatic Instance = null;

        public abstract bool HUDEnabled { get; set; }
        public abstract void CloseSysMenus_();
        public abstract void ShowUserHUD_(bool show = true);
        public abstract void DismissGadget_(string name = null);

        public static void EnableHUD(bool enable)
        {
            Instance.HUDEnabled = enable;
            DismissGadget();
            ShowUserHUD(false);

            if (!enable)
                CloseSysMenus();
            else
                ShowUserHUD();

        }

        public static void CloseSysMenus() => Instance.CloseSysMenus_();
        public static void ShowUserHUD(bool show = true) => Instance.ShowUserHUD_(show);
        public static void DismissGadget(string name = null) => Instance.DismissGadget_(name);
    }

    #endregion
    // -------------------------------------------------------------------
}
