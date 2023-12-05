/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Audio;
using Arteranos.Avatar;
using Arteranos.Core;
using Arteranos.XR;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;

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
                Resources.Load<GameObject>("UI/InApp/AvatarHitBox"), brain.transform);
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
                Resources.Load<GameObject>("UI/InApp/PrivacyBubble"), brain.transform);
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
        protected abstract IPAddress ExternalAddress_ { get; set; }
        protected abstract bool OpenPorts_ { get; set; }
        protected abstract Action<bool, string> OnClientConnectionResponse_ { get; set; }
        protected abstract IPAddress PublicIPAddress_ { get; set; }
        protected abstract string ServerHost_ { get; set; }
        protected abstract int ServerPort_ { get; set; }
        protected abstract event Action<ConnectivityLevel, OnlineLevel> OnNetworkStatusChanged_;
        protected abstract ConnectivityLevel GetConnectivityLevel_();
        protected abstract OnlineLevel GetOnlineLevel_();
        protected abstract IAvatarBrain GetOnlineUser_(UserID userID);
        protected abstract IAvatarBrain GetOnlineUser_(uint netId);
        protected abstract IEnumerable<IAvatarBrain> GetOnlineUsers_();
        protected abstract bool IsVerifiedUser_(UserID claimant);
        protected abstract void StartClient_(Uri connectionUri);
        protected abstract Task StartHost_(bool resetConnection = false);
        protected abstract void StartServer_();
        protected abstract Task StopHost_(bool loadOfflineScene);

        public static NetworkStatus Instance { get; set; }

        public static IPAddress ExternalAddress { get => Instance.ExternalAddress_; }

        public static IPAddress PublicIPAddress { get => Instance.PublicIPAddress_; }

        public static string ServerHost { get => Instance.ServerHost_; }

        public static int ServerPort { get => Instance.ServerPort_; }

        public static bool OpenPorts
        {
            get => Instance.OpenPorts_;
            set => Instance.OpenPorts_ = value;
        }
        public static Action<bool, string> OnClientConnectionResponse
        {
            get => Instance?.OnClientConnectionResponse_;
            set => Instance.OnClientConnectionResponse_ = value;
        }

        public static event Action<ConnectivityLevel, OnlineLevel> OnNetworkStatusChanged
        {
            add => Instance.OnNetworkStatusChanged_ += value;
            remove { if (Instance != null) Instance.OnNetworkStatusChanged_ -= value; }
        }

        public static ConnectivityLevel GetConnectivityLevel() 
            => Instance.GetConnectivityLevel_();
        public static OnlineLevel GetOnlineLevel() 
            => Instance?.GetOnlineLevel_() ?? OnlineLevel.Offline;
        public static void StartClient(Uri connectionUri) 
            => Instance.StartClient_(connectionUri);
        public static Task StartHost(bool resetConnection = false) 
            => Instance.StartHost_(resetConnection);
        public static void StartServer() 
            => Instance.StartServer_();
        public static Task StopHost(bool loadOfflineScene) 
            => Instance.StopHost_(loadOfflineScene);
        public static IAvatarBrain GetOnlineUser(UserID userID)
            => Instance.GetOnlineUser_(userID);
        public static IAvatarBrain GetOnlineUser(uint netId) 
            => Instance.GetOnlineUser_(netId);
        public static IEnumerable<IAvatarBrain> GetOnlineUsers() 
            => Instance.GetOnlineUsers_();
        public static bool IsVerifiedUser(UserID claimant) 
            => Instance.IsVerifiedUser_(claimant);


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
    #endregion
    // -------------------------------------------------------------------
}

namespace Arteranos.Web
{
    // -------------------------------------------------------------------
    #region Web helpers
    public abstract class ConnectionManager : MonoBehaviour
    {
        protected abstract Task<bool> ConnectToServer_(string serverURL);
        protected abstract void DeliverDisconnectReason_(string reason);
        protected abstract void ExpectConnectionResponse_();

        public static ConnectionManager Instance { get; protected set; }

        public static Task<bool> ConnectToServer(string serverURL) 
            => Instance.ConnectToServer_(serverURL);
        public static void DeliverDisconnectReason(string reason) 
            => Instance.DeliverDisconnectReason_(reason);
        public static void ExpectConnectionResponse()
            => Instance.ExpectConnectionResponse_();
    }

    public abstract class ServerSearcher : MonoBehaviour
    {
        protected abstract void InitiateServerTransition_(string worldURL);
        protected abstract void InitiateServerTransition_(string worldURL, Action<string, string> OnSuccessCallback, Action OnFailureCallback);

        public static ServerSearcher Instance { get; protected set; }

        public static void InitiateServerTransition(string worldURL)
            => Instance.InitiateServerTransition_(worldURL);
        public static void InitiateServerTransition(string worldURL, Action<string, string> OnSuccessCallback, Action OnFailureCallback)
            => Instance.InitiateServerTransition_(worldURL, OnSuccessCallback, OnFailureCallback);
    }
    public abstract class WorldGallery : MonoBehaviour
    {
        public static WorldGallery Instance { get; protected set; }

        protected abstract WorldInfo? GetWorldInfo_(string url);
        protected abstract Task<WorldInfo?> LoadWorldInfoAsync_(string url, CancellationToken token);
        protected abstract void PutWorldInfo_(string url, WorldInfo info);
        protected abstract void FavouriteWorld_(string url);
        protected abstract void UnfavoriteWorld_(string url);
        protected abstract bool IsWorldFavourited_(string url);
        protected abstract void BumpWorldInfo_(string url);

        public static WorldInfo? GetWorldInfo(string url)
            => Instance.GetWorldInfo_(url);
        public static Task<WorldInfo?> LoadWorldInfoAsync(string url, CancellationToken token)
            => Instance.LoadWorldInfoAsync_(url, token);
        public static void PutWorldInfo(string url, WorldInfo info)
            => Instance.PutWorldInfo_(url, info);
        public static void FavouriteWorld(string url)
            => Instance.FavouriteWorld_(url);
        public static void UnfavoriteWorld(string url)
            => Instance.UnfavoriteWorld_(url);
        public static bool IsWorldFavourited(string url)
            => Instance.IsWorldFavourited_(url);
        public static void BumpWorldInfo(string url)
            => Instance.BumpWorldInfo_(url);
    }

    public struct WorldData
    {
        public string worldURL;
        public string name;
        public UserID authorID;
        public byte[] icon;
        public ServerPermissions permissions;
    }


    public abstract class WorldTransition : MonoBehaviour
    {
        protected abstract Task<(Exception, Context)> PreloadWorldDataAsync_(string worldURL, bool forceReload = false);
        protected abstract bool IsWorldPreloaded_(string worldURL);
        protected abstract Task<(Exception, WorldData)> GetWorldDataAsync_(string worldURL);
        protected abstract Task<Exception> VisitWorldAsync_(string worldURL, bool forceReload = false);
        protected abstract Task MoveToOfflineWorld_();
        protected abstract Task MoveToOnlineWorld_(string worldURL);
        protected abstract Task EnterWorldAsync_(string worldURL, bool forceReload = false);
        protected abstract void EnterDownloadedWorld_(string worldABF);

        public static WorldTransition Instance { get; set; }

        public static Task<(Exception, WorldData)> GetWorldDataAsync(string worldURL)
            => Instance.GetWorldDataAsync_(worldURL);
        public static bool IsWorldPreloaded(string worldURL)
            => Instance.IsWorldPreloaded_(worldURL);
        public static Task MoveToOfflineWorld()
            => Instance.MoveToOfflineWorld_();
        public static Task MoveToOnlineWorld(string worldURL)
            => Instance.MoveToOnlineWorld_(worldURL);
        public static Task<(Exception, Context)> PreloadWorldDataAsync(string worldURL, bool forceReload = false)
            => Instance.PreloadWorldDataAsync_(worldURL, forceReload);
        public static Task<Exception> VisitWorldAsync(string worldURL, bool forceReload = false)
            => Instance.VisitWorldAsync_(worldURL, forceReload);
        public static Task EnterWorldAsync(string worldURL, bool forceReload = false)
            => Instance.EnterWorldAsync_(worldURL, forceReload);
        public static void EnterDownloadedWorld(string worldABF)
            => Instance.EnterDownloadedWorld_(worldABF);
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
                // Server's agreement is unchanged.
                if(serverTOSHash.SequenceEqual(sp.PrivacyTOSHash)) return true;
            }

            // Server uses an unknown TOS deviating from the standard TOS, needs to ask.
            if (si.UsesCustomTOS) return false;

            byte[] currentTOSHash = Crypto.SHA256(Utils.LoadDefaultTOS());

            // Only if the user's knowledge of the default TOS is up to date.
            return currentTOSHash.SequenceEqual(client.KnowsDefaultTOS);

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

            GameObject go = Instantiate(Resources.Load<GameObject>("UI/UI_AgreementDialogUI"));
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
            GameObject go = Instantiate(Resources.Load<GameObject>("UI/UI_Dialog"));
            return go.GetComponent<IDialogUI>();
        }

    }

    public class ProgressUIFactory : UIBehaviour
    {
        public static IProgressUI New()
        {
            GameObject go = Instantiate(Resources.Load<GameObject>("UI/UI_Progress"));
            return go.GetComponent<IProgressUI>();
        }
    }

    public class CreateAvatarUIFactory : UIBehaviour
    {
        public static ICreateAvatarUI New() 
        {
            GameObject go = Instantiate(Resources.Load<GameObject>("UI/UI_CreateAvatar"));
            return go.GetComponent<ICreateAvatarUI>();
        }
    }

    public class AvatarGalleryUIFactory : UIBehaviour
    {
        public static IAvatarGalleryUI New()
        {
            Transform t = XRControl.Instance.rigTransform;
            Vector3 position = t.position;
            Quaternion rotation= t.rotation;
            position += rotation * Vector3.forward * 1.5f;

            GameObject go = Instantiate(
                Resources.Load<GameObject>("UI/InApp/AvatarGalleryPedestal"),
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
                GameObject original = Resources.Load<GameObject>("UI/InApp/Nameplate");
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
            GameObject go = Instantiate(Resources.Load<GameObject>("UI/UI_TextMessage"));
            ITextMessageUI textMessageUI = go.GetComponent<ITextMessageUI>();
            textMessageUI.Receiver = receiver;
            return textMessageUI;
        }
    }

    public class KickBanUIFactory : UIBehaviour
    {
        public static IKickBanUI New(IAvatarBrain target)
        {
            GameObject go = Instantiate(Resources.Load<GameObject>("UI/UI_KickBan"));
            IKickBanUI kickBanUI = go.GetComponentInChildren<IKickBanUI>();
            kickBanUI.Target = target;
            return kickBanUI;
        }
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
