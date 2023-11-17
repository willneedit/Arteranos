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
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

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
    #region Avatar helpers

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
    #region Services helpers
    public static class NetworkStatus
    {
        public static INetworkStatus Instance { get; set; }

        public static IPAddress ExternalAddress { get => Instance.ExternalAddress; }

        public static IPAddress PublicIPAddress { get => Instance.PublicIPAddress; }

        public static string ServerHost { get => Instance.ServerHost; }

        public static int ServerPort { get => Instance.ServerPort; }

        public static bool OpenPorts
        {
            get => Instance.OpenPorts;
            set => Instance.OpenPorts = value;
        }
        public static Action<bool, string> OnClientConnectionResponse 
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
        public static OnlineLevel GetOnlineLevel() => Instance?.GetOnlineLevel() ?? OnlineLevel.Offline;
        public static void StartClient(Uri connectionUri) => Instance.StartClient(connectionUri);
        public static Task StartHost(bool resetConnection = false) => Instance.StartHost(resetConnection);
        public static void StartServer() => Instance.StartServer();
        public static Task StopHost(bool loadOfflineScene) => Instance.StopHost(loadOfflineScene);

        public static IAvatarBrain GetOnlineUser(UserID userID)
        {
            IEnumerable<IAvatarBrain> q =
                from entry in GameObject.FindGameObjectsWithTag("Player")
                where entry.GetComponent<IAvatarBrain>().UserID == userID
                select entry.GetComponent<IAvatarBrain>();

            return q.Count() > 0 ? q.First() : null;
        }

        public static IAvatarBrain GetOnlineUser(uint netId)
        {
            IEnumerable<IAvatarBrain> q =
                from entry in GameObject.FindGameObjectsWithTag("Player")
                where entry.GetComponent<IAvatarBrain>().NetID == netId
                select entry.GetComponent<IAvatarBrain>();

            return q.Count() > 0 ? q.First() : null;
        }

        public static IEnumerable<IAvatarBrain> GetOnlineUsers()
        {
            return from entry in GameObject.FindGameObjectsWithTag("Player")
                   select entry.GetComponent<IAvatarBrain>();

        }
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

        public static event Action<float[]> OnSampleReady
        {
            add => Instance.OnSampleReady += value;
            remove { if(Instance != null) Instance.OnSampleReady -= value; }
        }

        public static event Action<int, byte[]> OnSegmentReady
        {
            add => Instance.OnSegmentReady += value;
            remove { if(Instance != null) Instance.OnSegmentReady -= value; }
        }


        public static int? GetDeviceId() => Instance.GetDeviceId();
        public static void PushVolumeSettings() => Instance.PushVolumeSettings();
        public static void RenewMic() => Instance.RenewMic();

    }
    #endregion
    // -------------------------------------------------------------------
}

namespace Arteranos.Web
{
    // -------------------------------------------------------------------
    #region Web helpers
    public static class ConnectionManager
    {
        public static IConnectionManager Instance { get; set; }

        public static Task<bool> ConnectToServer(string serverURL) => Instance.ConnectToServer(serverURL);

        public static void DeliverDisconnectReason(string reason) => Instance.DeliverDisconnectReason(reason);
    }

    public static class ServerGallery
    {
        public static IServerGallery Instance { get; set; }

        public static void DeleteServerSettings(string url)
            => Instance?.DeleteServerSettings(url);
        public static ServerOnlineData? RetrieveServerSettings(string url)
            => Instance?.RetrieveServerSettings(url);
        public static void StoreServerSettings(string url, ServerOnlineData onlineData)
            => Instance?.StoreServerSettings(url, onlineData);
    }

    public static class ServerSearcher
    {
        public static IServerSearcher Instance { get; set; }

        public static void InitiateServerTransition(string worldURL) 
            => Instance.InitiateServerTransition(worldURL);
        public static void InitiateServerTransition(string worldURL, Action<string, string> OnSuccessCallback, Action OnFailureCallback) 
            => Instance.InitiateServerTransition(worldURL, OnSuccessCallback, OnFailureCallback);
    }
    public static class WorldGallery
    {
        public static IWorldGallery Instance { get; set; }

        public static void DeleteWorld(string url) 
            => Instance.DeleteWorld(url);
        public static (string, string) RetrieveWorld(string url, bool cached = false)
            => Instance.RetrieveWorld(url, cached);
        public static WorldMetaData RetrieveWorldMetaData(string url)
            => Instance.RetrieveWorldMetaData(url);
        public static bool StoreWorld(string url)
            => Instance.StoreWorld(url);
        public static void StoreWorldMetaData(string url, WorldMetaData worldMetaData) 
            => Instance.StoreWorldMetaData(url, worldMetaData);
    }

    public class WorldTransition
    {
        public static IWorldTransition Instance { get; set; }

        public static Task<(Exception, WorldData)> GetWorldDataAsync(string worldURL) 
            => Instance.GetWorldDataAsync(worldURL);
        public static bool IsWorldPreloaded(string worldURL) 
            => Instance.IsWorldPreloaded(worldURL);
        public static Task MoveToOfflineWorld() 
            => Instance.MoveToOfflineWorld();
        public static Task MoveToOnlineWorld(string worldURL) 
            => Instance.MoveToOnlineWorld(worldURL);
        public static Task<(Exception, Context)> PreloadWorldDataAsync(string worldURL, bool forceReload = false) 
            => Instance.PreloadWorldDataAsync(worldURL, forceReload);
        public static Task<Exception> VisitWorldAsync(string worldURL, bool forceReload = false) 
            => Instance.VisitWorldAsync(worldURL, forceReload);
        public static Task EnterWorldAsync(string worldURL, bool forceReload = false) 
            => Instance.EnterWorldAsync(worldURL, forceReload);
        public static void EnterDownloadedWorld(string worldABF)
            => Instance.EnterDownloadedWorld(worldABF);
    }

    public static class WorldDownloaderLow
    {
        public static void EnterEmbeddedWorld(string path)
        {
            SceneManager.LoadScene(path);
            MoveToDownloadedWorld();
        }

        public static void MoveToDownloadedWorld() => XRControl.Instance.MoveRig();

    }

    #endregion
    // -------------------------------------------------------------------
}

namespace Arteranos.UI
{
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
