/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using Arteranos.XR;
using System;
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
    #region Avatar interfaces

    public static class NetMuteStatus
    {
        public const int OK = 0;
        public const int SelfMuted = (1 << 0); // Muted by the client himself
        public const int Gagged = (1 << 1); // Muted by the administrator/event host/...
    }

    public interface IAvatarBrain
    {
        string AvatarURL { get; }
        int ChatOwnID { get; }
        uint NetID { get; }
        byte[] UserHash { get; }
        string Nickname { get; }
        int NetMuteStatus { get; set; }
        bool IsOwned { get; }
        bool ClientMuted { get; set; }
        IAvatarLoader Body { get; }

        event Action<string> OnAvatarChanged;
        event Action<int> OnNetMuteStatusChanged;

        void OnDestroy();
        void OnStartClient();
        void OnStartServer();
        void OnStopClient();
    }

    #endregion
    // -------------------------------------------------------------------
    #region Avatar helpers
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
    #endregion
    // -------------------------------------------------------------------
}

namespace Arteranos.XR
{
    // -------------------------------------------------------------------
    #region XR interfaces
    public interface IXRControl
    {
        GameObject Me { get; set; }
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

        public static GameObject Me
        {
            get => Instance.Me;
            set => Instance.Me = value;
        }
    }
    #endregion
    // -------------------------------------------------------------------
}