/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using System;
using System.ComponentModel;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

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

        event Action<ConnectivityLevel, OnlineLevel> OnNetworkStatusChanged;

        ConnectivityLevel GetConnectivityLevel();
        OnlineLevel GetOnlineLevel();
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

        public static ConnectivityLevel GetConnectivityLevel() => Instance.GetConnectivityLevel();
        public static OnlineLevel GetOnlineLevel() => Instance.GetOnlineLevel();
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
        void StartHost();
        void StartServer();
        void StopHost();
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
        public static void StartHost() => Instance.StartHost();
        public static void StartServer() => Instance.StartServer();
        public static void StopHost() => Instance.StopHost();
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
    #endregion
    // -------------------------------------------------------------------
}

namespace Arteranos.XR
{
    // -------------------------------------------------------------------
    #region XR interfaces
    public interface IXRControl
    {
        // XROrigin CurrentVRRig { get; }
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
    public class XRControl
    {
        public static IXRControl Instance { get; set; }
    }
    #endregion
    // -------------------------------------------------------------------
}