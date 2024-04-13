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
using Arteranos.XR;
using Ipfs;
using Ipfs.Core.Cryptography.Proto;
using Ipfs.CoreApi;
using Ipfs.Engine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using AsyncOperation = UnityEngine.AsyncOperation;

namespace Arteranos.Avatar
{
    // -------------------------------------------------------------------
    #region Avatar constants

    public static class AppearanceStatus
    {
        public const int OK = 0;

        // Muted by the user himself
        public const int Muting = (1 << 0);

        // (Local only) Muted by you
        public const int Muted = (1 << 1);

        // Muted by the administrator/event host/...
        public const int Gagged = (1 << 2);


        // Reasons for a user is silent
        public const int MASK_AUDIO = 0x0000FFFF;


        // (Local Only) Invisible because of being too close
        public const int Bubbled = (1 << 16);

        // (Local Only) User is blocked by you
        public const int Blocked = (1 << 17);

        // (Local Only) User is blocking you, therefore he's invisible to you too.
        public const int Blocking = (1 << 18);

        // Made invisible by the administrator/event host/...
        public const int Ghosted = (1 << 19);


        // Reasons fot a user is invisible
        public const int MASK_VIDEO = 0x7FFF0000;


        // Reasons which are individual causes, not net-wide ones
        public const int MASK_LOCAL = (Muted | Bubbled | Blocked | Blocking);

        public static bool IsSilent(int status) => (status != OK); // Being invisible implies being silent, too.

        public static bool IsInvisible(int status) => ((status & MASK_VIDEO) != OK);

        public static bool IsLocal(int status) => ((status & MASK_LOCAL) != OK);
    }

    #endregion
    // -------------------------------------------------------------------
    #region Avatar factories

    public class AvatarHitBoxFactory : MonoBehaviour
    {
        public static IHitBox New(IAvatarBrain brain)
        {
            GameObject go = Instantiate(
                BP.I.InApp.AvatarHitBox, brain.transform);
            IHitBox hitBox = go.GetComponent<IHitBox>();
            hitBox.Brain = brain;

            return hitBox;
        }
    }

    public class BubbleCoordinatorFactory : MonoBehaviour
    {
        public static IBubbleCoordinator New(IAvatarBrain brain)
        {
            GameObject go = Instantiate(
                BP.I.InApp.PrivacyBubble, brain.transform);
            IBubbleCoordinator bc = go.GetComponent<IBubbleCoordinator>();
            bc.Brain = brain;
            return bc;
        }
    }
    #endregion
    // -------------------------------------------------------------------
}

namespace Arteranos.Services
{
    // -------------------------------------------------------------------
    #region Services constants
    public enum ConnectivityLevel
    {
        [Description("Unconnected")]
        Unconnected = 0,
        [Description("Firewalled")]
        Restricted,
        [Description("Public")]
        Unrestricted
    }
    public enum OnlineLevel
    {
        [Description("Offline")]
        Offline = 0,
        [Description("Client")]
        Client,
        [Description("Server")]
        Server,
        [Description("Host")]
        Host
    }

    #endregion
    // -------------------------------------------------------------------
    #region Services helpers
    public abstract class NetworkStatus : MonoBehaviour
    {
        protected abstract bool OpenPorts_ { get; set; }
        protected abstract IPAddress ExternalAddress_ { get; set; }
        protected abstract IPAddress PublicIPAddress_ { get; set; }
        protected abstract Action<bool, string> OnClientConnectionResponse_ { get; set; }
        protected abstract MultiHash RemotePeerId_ { get; set; }
        protected abstract event Action<ConnectivityLevel, OnlineLevel> OnNetworkStatusChanged_;
        protected abstract ConnectivityLevel GetConnectivityLevel_();
        protected abstract OnlineLevel GetOnlineLevel_();
        protected abstract bool IsClientConnecting_ { get; }
        protected abstract bool IsClientConnected_ { get; }
        protected abstract IAvatarBrain GetOnlineUser_(UserID userID);
        protected abstract IAvatarBrain GetOnlineUser_(uint netId);
        protected abstract IEnumerable<IAvatarBrain> GetOnlineUsers_();
        protected abstract void StartClient_(Uri connectionUri);
        protected abstract Task StartHost_(bool resetConnection = false);
        protected abstract Task StartServer_();
        protected abstract Task StopHost_(bool loadOfflineScene);

        public static NetworkStatus Instance { get; set; }

        public static IPAddress ExternalAddress => Instance.ExternalAddress_;

        public static IPAddress PublicIPAddress => Instance.PublicIPAddress_;

        public static bool OpenPorts
        {
            get => Instance.OpenPorts_;
            set => Instance.OpenPorts_ = value;
        }
        public static Action<bool, string> OnClientConnectionResponse
        {
            get => Instance ? Instance.OnClientConnectionResponse_ : null;
            set => Instance.OnClientConnectionResponse_ = value;
        }

        public static event Action<ConnectivityLevel, OnlineLevel> OnNetworkStatusChanged
        {
            add => Instance.OnNetworkStatusChanged_ += value;
            remove { if (Instance != null) Instance.OnNetworkStatusChanged_ -= value; }
        }

        public static MultiHash RemotePeerId
        {
            get => Instance.RemotePeerId_;
            set => Instance.RemotePeerId_ = value;
        }
        public static ConnectivityLevel GetConnectivityLevel() 
            => Instance.GetConnectivityLevel_();
        public static OnlineLevel GetOnlineLevel() 
            => Instance ? Instance.GetOnlineLevel_() : OnlineLevel.Offline;
        public static bool IsClientConnecting => Instance.IsClientConnecting_;
        public static bool IsClientConnected => Instance.IsClientConnected_;
        public static void StartClient(Uri connectionUri) 
            => Instance.StartClient_(connectionUri);
        public static Task StartHost(bool resetConnection = false) 
            => Instance.StartHost_(resetConnection);
        public static Task StartServer() 
            => Instance.StartServer_();
        public static Task StopHost(bool loadOfflineScene) 
            => Instance.StopHost_(loadOfflineScene);
        public static IAvatarBrain GetOnlineUser(UserID userID)
            => Instance.GetOnlineUser_(userID);
        public static IAvatarBrain GetOnlineUser(uint netId) 
            => Instance.GetOnlineUser_(netId);
        public static IEnumerable<IAvatarBrain> GetOnlineUsers() 
            => Instance.GetOnlineUsers_();
    }

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
        public abstract IpfsEngine Ipfs_ { get; }
        public abstract Peer Self_ { get; }
        public abstract SignKey ServerKeyPair_ { get; }
        public abstract Cid IdentifyCid_ { get; protected set; }

        public abstract event Action<IPublishedMessage> OnReceivedHello_;
        public abstract event Action<IPublishedMessage> OnReceivedServerDirectMessage_;

        public abstract Task<IPAddress> GetPeerIPAddress_(MultiHash PeerID, CancellationToken token = default);
        public abstract Task FlipServerDescription_(bool reload);
        public abstract Task SendServerHello_();
        public abstract Task SendServerOnlineData_();
        public abstract Task SendServerDirectMessage_(string peerId, PeerMessage message);
        public abstract Task PinCid_(Cid cid, bool pinned, CancellationToken token = default);

        public static IPFSService Instance { get; protected set; }

        public static Peer Self 
            => Instance.Self_;
        public static SignKey ServerKeyPair
            => Instance.ServerKeyPair_;
        public static PublicKey ServerPublicKey
            => ServerKeyPair.PublicKey;
        public static Cid IdentifyCid
            => Instance.IdentifyCid_;
        public static Task<IPAddress> GetPeerIPAddress(MultiHash PeerID, CancellationToken token = default)
            => Instance.GetPeerIPAddress_(PeerID, token);
        public static Task FlipServerDescription(bool reload)
            => Instance.FlipServerDescription_(reload);
        public static Task SendServerHello()
            => Instance.SendServerHello_();
        public static Task SendServerOnlineData()
            => Instance.SendServerOnlineData_();
        public static Task SendServerDirectMessage(string peerId, PeerMessage message)
            => Instance.SendServerDirectMessage_(peerId, message);

        public static Task PinCid(Cid cid, bool pinned, CancellationToken cancel = default)
            => Instance.PinCid_(cid, pinned, cancel);
        public static Task<IEnumerable<Cid>> ListPinned(CancellationToken cancel = default)
            => Instance.Ipfs_.Pin.ListAsync(cancel);
        public static Task<Stream> ReadFile(string path, CancellationToken cancel = default)
            => Instance.Ipfs_.FileSystem.ReadFileAsync(path, cancel);
        public static Task<Stream> Get(string path, CancellationToken cancel = default)
            => Instance.Ipfs_.FileSystem.GetAsync(path, cancel: cancel);
        public static Task<IFileSystemNode> AddStream(Stream stream, string name = "", AddFileOptions options = null, CancellationToken cancel = default)
            => Instance.Ipfs_.FileSystem.AddAsync(stream, name, options, cancel);
        public static Task<IFileSystemNode> ListFile(string path, CancellationToken cancel = default)
            => Instance.Ipfs_.FileSystem.ListFileAsync(path, cancel);
        public static Task<IFileSystemNode> AddDirectory(string path, bool recursive = true, AddFileOptions options = null, CancellationToken cancel = default)
            => Instance.Ipfs_.FileSystem.AddDirectoryAsync(path, recursive, options, cancel);

        public static MultiAddress GetMultiAddress(IPAddress addr, int port, MultiHash peer_id)
        {
            StringBuilder sb = new();
            sb.Append(addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6
                ? "/ip6/"
                : "/ip4/");
            sb.Append(addr);
            sb.Append("/tcp/");
            sb.Append(port);
            if(peer_id != null)
            {
                sb.Append("/p2p/");
                sb.Append(peer_id);
            }

            return sb.ToString();
        }

        public static (IPAddress, int, MultiHash) ParseMultiAddress(MultiAddress maddr)
        {
            IPAddress addr = IPAddress.None;
            int port = 0;
            MultiHash peer_id = null;

            string[] parts = maddr.ToString().Split('/');

            for(int i = 1; i < parts.Length; i++)
            {
                switch(parts[i])
                {
                    case "ip4":
                    case "ip6":
                        i++;
                        addr = IPAddress.Parse(parts[i]);
                        break;
                    case "tcp":
                    case "udp":
                        i++;
                        port = int.Parse(parts[i]);
                        break;
                    case "p2p":
                    case "ipfs":
                        i++;
                        peer_id = new(parts[i]);
                        break;
                    default:
                        throw new ArgumentException("Unsupported or malformed Multiaddress");
                }
            }
            return (addr, port, peer_id);
        }
    }

    public abstract class TransitionProgressStatic : MonoBehaviour
    {
        public static TransitionProgressStatic Instance;

        public abstract void OnProgressChanged(float progress, string progressText);

        public static IEnumerator TransitionFrom()
        {
            ScreenFader.StartFading(1.0f);
            yield return new WaitForSeconds(0.5f);

            AsyncOperation ao = SceneManager.LoadSceneAsync("Transition");
            while (!ao.isDone) yield return null;


            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            XRControl.Instance.MoveRig();

            ScreenFader.StartFading(0.0f);

            yield return new WaitUntil(() => Instance);

            SysMenuStatic.EnableHUD(false);
        }

        // NOTE: Needs preloaded world! Just deploys the sceneloader which it uses
        // the current preloaded world asset bundle!
        public static IEnumerator TransitionTo(Cid WorldCid, string WorldName)
        {
            ScreenFader.StartFading(1.0f);
            yield return new WaitForSeconds(0.5f);

            yield return MoveToPreloadedWorld(WorldCid, WorldName);

            ScreenFader.StartFading(0.0f);

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

                XRControl.Instance.MoveRig();
            }
            else
                yield return EnterDownloadedWorld();

            SettingsManager.WorldCid = WorldCid;
            SettingsManager.WorldName = WorldName;
        }

        public static IEnumerator EnterDownloadedWorld()
        {
            string worldABF = Core.Operations.WorldDownloader.CurrentWorldAssetBundlePath;

            Debug.Log($"Download complete, world={worldABF}");

            yield return null;

            bool done = false;

            // Deploy the scene loader.
            GameObject go = new("_SceneLoader");
            go.AddComponent<Persistence>();
            Web.SceneLoader sl = go.AddComponent<Web.SceneLoader>();
            sl.OnFinishingSceneChange += () =>
            {
                XRControl.Instance.MoveRig();
                done = true;
            };

            sl.Name = worldABF;

            yield return new WaitUntil(() => done);
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
            Transform t = XRControl.Instance.rigTransform;
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

namespace Arteranos.XR
{
    // -------------------------------------------------------------------
    #region XR helpers
    public static class XRControl
    {
        public static IXRControl Instance { get; set; }

        public static IAvatarBrain Me
        {
            get => Instance?.Me;
            set => Instance.Me = value;
        }
    }

    public static class ScreenFader
    {
        public static IXRVisualConfigurator Instance { get; set; }

        public static void StartFading(float opacity, float duration = 0.5f)
            => Instance?.StartFading(opacity, duration);
    }
    #endregion
    // -------------------------------------------------------------------
}
