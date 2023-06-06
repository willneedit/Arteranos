/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Avatar;
using Arteranos.Core;
using Arteranos.XR;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;

#pragma warning disable IDE1006 // Because Unity's more relaxed naming convention

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
    #region Avatar interfaces

    public struct ShitListEntry
    {
        public UserID userID;
        public int state;
    }

    public interface IAvatarBrain
    {
        string AvatarURL { get; }
        int ChatOwnID { get; }
        uint NetID { get; }
        string Nickname { get; }
        int AppearanceStatus { get; set; }
        bool isOwned { get; }
        IAvatarLoader Body { get; }
        GameObject gameObject { get; }
        UserID UserID { get; }

        event Action<string> OnAvatarChanged;
        event Action<int> OnAppearanceStatusChanged;

        void BlockUser(IAvatarBrain receiver, bool blocking = true);
        void LogDebug(object message);
        void LogError(object message);
        void LogWarning(object message);
        void NotifyBubbleBreached(IAvatarBrain touchy, bool isFriend, bool entered);
        void OfferFriendship(IAvatarBrain receiver, bool offering = true);
        void SaveSocialStates(IAvatarBrain receiver, int state);
        void SetAppearanceStatusBit(int ASBit, bool set);
        void UpdateReflectiveSSEffects(IAvatarBrain receiver, int state);
        void UpdateSSEffects(IAvatarBrain receiver, int state);
        void UpdateToGlobalUserID(IAvatarBrain receiver, UserID globalUserID);
    }

    public interface IHitBox
    {
        GameObject gameObject { get; }
        IAvatarBrain Brain { get; set; }

        bool interactable { get; set; }

        void OnTargeted(bool inSight);
    }

    public interface IBubbleCoordinator
    {
        IAvatarBrain Brain { get; set; }

        void ChangeBubbleSize(float diameter, bool isFriend);
        void NotifyTrigger(IAvatarBrain touchy, bool isFriend, bool entered);
    }



    #endregion
    // -------------------------------------------------------------------
    #region Avatar helpers

    public class AvatarHitBoxFactory : MonoBehaviour
    {
        public static IHitBox New(IAvatarBrain brain)
        {
            GameObject go = Instantiate(
                Resources.Load<GameObject>("UI/InApp/AvatarHitBox"), brain.gameObject.transform);
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
                Resources.Load<GameObject>("UI/InApp/PrivacyBubble"), brain.gameObject.transform);
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
    #region Services interfaces

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

    public interface INetworkStatus
    {
        IPAddress ExternalAddress { get; }
        bool OpenPorts { get; set; }
        bool enabled { get; set; }
        Action<bool> OnClientConnectionResponse { get; set; }

        event Action<ConnectivityLevel, OnlineLevel> OnNetworkStatusChanged;

        ConnectivityLevel GetConnectivityLevel();
        OnlineLevel GetOnlineLevel();
        void StartClient(Uri connectionUri);
        void StartHost();
        void StartServer();
        void StopHost();
    }

    public interface IAudioManager
    {
        AudioMixerGroup MixerGroupEnv { get; }
        AudioMixerGroup MixerGroupVoice { get; }
        float VolumeEnv { get; set; }
        float VolumeMaster { get; set; }
        float VolumeVoice { get; set; }
        bool enabled { get; set; }
        float MicGain { get; set; }
        int MicAGCLevel { get; set; }
        bool MuteSelf { get; set; }

        event Action<short> OnJoinedChatroom;
        event Action<float[]> OnSampleReady;

        int? GetDeviceId();
        void JoinChatroom(object data = null);
        void LeaveChatroom(object data = null);
        void MuteOther(short peerID, bool muted);
        bool MuteOther(short peerID);
        void PushVolumeSettings();
        void RenewMic();
    }

    #endregion
    // -------------------------------------------------------------------
    #region Services helpers
    public static class NetworkStatus
    {
        public static INetworkStatus Instance { get; set; }

        public static IPAddress ExternalAddress { get => Instance.ExternalAddress; }
        public static bool OpenPorts
        {
            get => Instance.OpenPorts;
            set => Instance.OpenPorts = value;
        }
        public static Action<bool> OnClientConnectionResponse 
        {
            get => Instance?.OnClientConnectionResponse;
            set => Instance.OnClientConnectionResponse = value;
        }

        public static event Action<ConnectivityLevel, OnlineLevel> OnNetworkStatusChanged
        {
            add => Instance.OnNetworkStatusChanged += value;
            remove { if(Instance != null) Instance.OnNetworkStatusChanged -= value; }
        }

        public static ConnectivityLevel GetConnectivityLevel() => Instance.GetConnectivityLevel();
        public static OnlineLevel GetOnlineLevel() => Instance.GetOnlineLevel();
        public static void StartClient(Uri connectionUri) => Instance.StartClient(connectionUri);
        public static void StartHost() => Instance.StartHost();
        public static void StartServer() => Instance.StartServer();
        public static void StopHost() => Instance.StopHost();
    }

    public static class AudioManager
    {
        public static IAudioManager Instance { get; set; }

        public static AudioMixerGroup MixerGroupEnv { get => Instance.MixerGroupEnv; }
        public static AudioMixerGroup MixerGroupVoice { get => Instance.MixerGroupVoice; }
        public static float VolumeEnv 
        {
            get => Instance.VolumeEnv;
            set => Instance.VolumeEnv = value;
        }
        public static float VolumeMaster 
        {
            get => Instance.VolumeMaster;
            set => Instance.VolumeMaster = value;
        }
        public static float VolumeVoice
        {
            get => Instance.VolumeVoice;
            set => Instance.VolumeVoice = value;
        }
        public static float MicGain
        {
            get => Instance.MicGain;
            set => Instance.MicGain = value;
        }
        public static int MicAGCLevel 
        {
            get => Instance.MicAGCLevel;
            set => Instance.MicAGCLevel = value;
        }


        public static event Action<short> OnJoinedChatroom
        { 
            add => Instance.OnJoinedChatroom += value;
            remove=> Instance.OnJoinedChatroom -= value;
        }
        public static event Action<float[]> OnSampleReady
        {
            add => Instance.OnSampleReady += value;
            remove { if(Instance != null) Instance.OnSampleReady -= value; }
        }

        public static int? GetDeviceId() => Instance.GetDeviceId();
        public static void JoinChatroom(object data = null) => Instance.JoinChatroom(data);
        public static void LeaveChatroom(object data = null) => Instance?.LeaveChatroom(data);
        public static void PushVolumeSettings() => Instance.PushVolumeSettings();
        public static void RenewMic() => Instance.RenewMic();

    }
    #endregion
    // -------------------------------------------------------------------
}

namespace Arteranos.Web
{
    // -------------------------------------------------------------------
    #region Web interfaces
    public interface IConnectionManager
    {
        Task<bool> ConnectToServer(string serverURL);
    }

    public interface IServerGallery
    {
        void DeleteServerSettings(string url);
        Task<(string, ServerMetadataJSON)> DownloadServerMetadataAsync(string url, int timeout = 20);
        void DownloadServerMetadataAsync(string url, Action<string, ServerMetadataJSON> callback, int timeout = 20);
        ServerSettingsJSON RetrieveServerSettings(string url);
        void StoreServerSettings(string url, ServerSettingsJSON serverSettings);
    }

    public interface IWorldGallery
    {
        void DeleteWorld(string url);
        (string, string) RetrieveWorld(string url, bool cached = false);
        WorldMetaData RetrieveWorldMetaData(string url);
        bool StoreWorld(string url);
        void StoreWorldMetaData(string url, WorldMetaData worldMetaData);
    }

    public interface IWorldTransition
    {
        void InitiateTransition(string url, Action failureCallback = null, Action successCallback = null);
    }

    #endregion
    // -------------------------------------------------------------------
    #region Web helpers
    public static class ConnectionManager
    {
        public static IConnectionManager Instance { get; set; }

        public static Task<bool> ConnectToServer(string serverURL) => Instance.ConnectToServer(serverURL);
    }

    public static class ServerGallery
    {
        public static IServerGallery Instance { get; set; }

        public static void DeleteServerSettings(string url) => Instance.DeleteServerSettings(url);
        public static Task<(string, ServerMetadataJSON)> DownloadServerMetadataAsync(string url, int timeout = 20)
            => Instance.DownloadServerMetadataAsync(url, timeout);
        public static void DownloadServerMetadataAsync(string url, Action<string, ServerMetadataJSON> callback, int timeout = 20)
            => Instance.DownloadServerMetadataAsync(url, callback, timeout);
        public static ServerSettingsJSON RetrieveServerSettings(string url) => Instance.RetrieveServerSettings(url);
        public static void StoreServerSettings(string url, ServerSettingsJSON serverSettings) => Instance.StoreServerSettings(url, serverSettings);
    }

    public static class WorldGallery
    {
        public static IWorldGallery Instance { get; set; }

        public static void DeleteWorld(string url) => Instance.DeleteWorld(url);
        public static (string, string) RetrieveWorld(string url, bool cached = false) => Instance.RetrieveWorld(url, cached);
        public static WorldMetaData RetrieveWorldMetaData(string url) => Instance.RetrieveWorldMetaData(url);
        public static bool StoreWorld(string url) => Instance.StoreWorld(url);
        public static void StoreWorldMetaData(string url, WorldMetaData worldMetaData) => Instance.StoreWorldMetaData(url, worldMetaData);
    }

    public static class WorldTransition
    {
        public static IWorldTransition Instance { get; set; }

        public static void InitiateTransition(string url, Action failureCallback = null, Action successCallback = null) 
            => Instance.InitiateTransition(url, failureCallback, successCallback);
    }

    #endregion
    // -------------------------------------------------------------------
}

namespace Arteranos.UI
{
    // -------------------------------------------------------------------
    #region UI interfaces
    public interface IDialogUI
    {
        public string Text { get; set; }
        public string[] Buttons { get; set; }
        public void Close();

        event Action<int> OnDialogDone;

        Task<int> PerformDialogAsync(string text, string[] buttons);
    }

    public interface IProgressUI
    {
        bool AllowCancel { get; set; }
        AsyncOperationExecutor<Context> Executor { get; set; }
        Context Context { get; set; }

        event Action<Context> Completed;
        event Action<Exception, Context> Faulted;
    }

    public interface ICreateAvatarUI
    {

    }

    public interface IAvatarGalleryUI
    {

    }

    public interface INameplateUI
    {
        IAvatarBrain Bearer { get; set; }
        GameObject gameObject { get; }
        bool enabled { get; set; }
    }


    #endregion
    // -------------------------------------------------------------------
    #region UI factories
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
                go = Instantiate(
                    Resources.Load<GameObject>("UI/InApp/Nameplate"),
                    bearer.transform
                    );
                nameplate = go.GetComponent<INameplateUI>();
            }

            nameplate.Bearer = bearer.GetComponent<IAvatarBrain>();
            go.SetActive(true);

            return nameplate;
        }
    }
    #endregion
    // -------------------------------------------------------------------
}

namespace Arteranos.XR
{
    // -------------------------------------------------------------------
    #region XR interfaces
    public interface IXRControl
    {
        IAvatarBrain Me { get; set; }
        Vector3 CameraLocalOffset { get; }
        bool UsingXR { get; }
        bool enabled { get; set; }
        public float EyeHeight { get; set; }
        public float BodyHeight { get; set; }
        GameObject gameObject { get; }
        Transform rigTransform { get; }
        Transform cameraTransform { get; }
        Vector3 heightAdjustment { get; }

        public void ReconfigureXRRig();
        void FreezeControls(bool value);
        void MoveRig(Vector3 startPosition, Quaternion startRotation);

        event Action<bool> XRSwitchEvent;
    }
    #endregion
    // -------------------------------------------------------------------
    #region XR helpers
    public static class XRControl
    {
        public static IXRControl Instance { get; set; }

        public static IAvatarBrain Me
        {
            get => Instance.Me;
            set => Instance.Me = value;
        }
    }
    #endregion
    // -------------------------------------------------------------------
}

namespace Arteranos.Social
{
    // -------------------------------------------------------------------
    #region Social constants
    public static class SocialState
    {
        public const int None = 0;

        // You offered your friendship to the targeted user.
        public const int Friend_offered     = (1 << 0);

        // You blocked the targeted user.
        public const int Blocked            = (1 << 8);

        public static bool IsFriends(int you, int him) 
            => (you == Friend_offered) && (him == Friend_offered);


    }
    #endregion
    // -------------------------------------------------------------------

}