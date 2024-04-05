/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using UnityEngine;

using Mono.Nat;
using System;
using System.Threading.Tasks;
using Arteranos.Core;
using Mirror;

using System.Net;
using Arteranos.Web;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Text.RegularExpressions;
using Arteranos.Avatar;
using System.Linq;
using Arteranos.Core.Operations;
using Ipfs;

namespace Arteranos.Services
{
    public partial class NetworkStatusImpl : NetworkStatus
    {

        private INatDevice device = null;
        protected override IPAddress ExternalAddress_ { get; set; } = IPAddress.None;
        protected override MultiHash RemotePeerId_ { get; set; } = null;

        protected override event Action<ConnectivityLevel, OnlineLevel> OnNetworkStatusChanged_;
        protected override Action<bool, string> OnClientConnectionResponse_ { get => m_OnClientConnectionResponse; set => m_OnClientConnectionResponse = value; }

        protected override bool OpenPorts_
        {
            get => m_OpenPorts;
            set
            {
                bool old = m_OpenPorts;
                if(old != value)
                {
                    m_OpenPorts = value;
                    if(m_OpenPorts) OpenPortsAsync();
                    else ClosePortsAsync();
                }
            }
        }

        private Action<bool, string> m_OnClientConnectionResponse = null;

        public bool ServerPortPublic = false;
        public bool MetadataPortPublic = false;

        private ConnectivityLevel CurrentConnectivityLevel = ConnectivityLevel.Unconnected;
        private OnlineLevel CurrentOnlineLevel = OnlineLevel.Offline;
        private bool m_OpenPorts = false;

        private NetworkManager manager = null;
        private TelepathyTransport transport = null;
        private void Awake()
        {
            manager = FindObjectOfType<NetworkManager>(true);
            transport = FindObjectOfType<TelepathyTransport>(true);

            Instance = this;
        }

        private void OnDestroy()
        {
            ClosePortsAsync();
            Instance = null;
        }

        // -------------------------------------------------------------------
        #region Running
        protected override ConnectivityLevel GetConnectivityLevel_()
        {
            if(Application.internetReachability == NetworkReachability.NotReachable)
                return ConnectivityLevel.Unconnected;

            return (ServerPortPublic && MetadataPortPublic)
                ? ConnectivityLevel.Unrestricted
                : ConnectivityLevel.Restricted;
        }

        protected override OnlineLevel GetOnlineLevel_()
        {
            if(!NetworkClient.active && !NetworkServer.active)
                return OnlineLevel.Offline;

            if(NetworkClient.active && NetworkServer.active)
                return OnlineLevel.Host;

            return NetworkClient.active
                ? OnlineLevel.Client
                : OnlineLevel.Server;
        }

        protected override bool IsClientConnecting_ => NetworkClient.isConnecting;

        protected override bool IsClientConnected_ => NetworkClient.isConnected;

        void OnEnable()
        {
            Debug.Log("Setting up NAT gateway configuration");

            IEnumerator RefreshDiscovery()
            {
                while(true)
                {
                    // No sense for router and IP detection if the computer's network cable is unplugged
                    // and in its airplane mode.
                    if (GetConnectivityLevel_() == ConnectivityLevel.Unconnected)
                    {
                        yield return new WaitForSeconds(10);
                    }
                    else
                    {
                        // Needs to be refreshed anytime, because the router invalidates port forwarding
                        // if the connected device falls idle, or catatonic.
                        NatUtility.StartDiscovery();

                        // Wait for refresh...
                        yield return new WaitForSeconds(500);
                    }


                    NatUtility.StopDiscovery();
                }
            }

            NatUtility.DeviceFound += DeviceFound;

            StartCoroutine(RefreshDiscovery());
        }

        private void OnDisable()
        {
            Debug.Log("Shutting down NAT gateway configuration");

            NatUtility.DeviceFound -= DeviceFound;

            StopAllCoroutines();

            ClosePortsAsync();

            NatUtility.StopDiscovery();
        }

        private void Update()
        {
            ConnectivityLevel c1 = GetConnectivityLevel_();
            OnlineLevel c2 = GetOnlineLevel_();

            if(CurrentConnectivityLevel != c1 || CurrentOnlineLevel != c2)
                OnNetworkStatusChanged_?.Invoke(c1, c2);

            if(CurrentOnlineLevel == OnlineLevel.Client && c2 == OnlineLevel.Offline)
                OnRemoteDisconnected();

            CurrentConnectivityLevel = c1;
            CurrentOnlineLevel = c2;
        }

        #endregion
        // -------------------------------------------------------------------
        #region Connectivity and UPnP

        private async void DeviceFound(object sender, DeviceEventArgs e)
        {
            // For some reason, my Fritz!Box reponds twice, for two WAN ports,
            // all with the same external IP address?
            if(device != null) return;

            device = e.Device;

            ExternalAddress_ = await device.GetExternalIPAsync();

            Debug.Log($"Device found : {device.NatProtocol}");
            Debug.Log($"  Type       : {device.GetType().Name}");
            Debug.Log($"  External IP: {ExternalAddress_}");

            OpenPortsAsync();
        }

        private async Task<bool> OpenPortAsync(int port)
        {
            // TCP, and internal and external ports as the same.
            Mapping mapping = new(Protocol.Tcp, port, port);

            try
            {
                await device.CreatePortMapAsync(mapping);
                return true;
            }
            catch(Exception ex)
            {
                Debug.LogWarning($"Failed to create a port mapping for {port}");
                Debug.LogException(ex);
                return false;
            }
        }

        private async void ClosePortAsync(int port)
        {
            // TCP, and internal and external ports as the same.
            Mapping mapping = new(Protocol.Tcp, port, port);

            try
            {
                await device.DeletePortMapAsync(mapping);
            }
            catch
            {
                Debug.Log($"Failed to delete a port mapping for {port}... but it's okay.");
            }
        }

        public async void OpenPortsAsync()
        {
            // No point to open the ports if we're not supposed to.
            if(!OpenPorts_) return;

            Debug.Log("Opening ports in the router");

            Server ss = SettingsManager.Server;

            ServerPortPublic = await OpenPortAsync(ss.ServerPort);
            MetadataPortPublic = await OpenPortAsync(ss.MetadataPort);
        }

        public void ClosePortsAsync()
        {
            Debug.Log("Closing ports in the router, if there's need to do.");

            Server ss = SettingsManager.Server;

            if(ServerPortPublic)
                ClosePortAsync(ss.ServerPort);

            if(MetadataPortPublic)
                ClosePortAsync(ss.MetadataPort);

            ServerPortPublic = false;
            MetadataPortPublic = false;
        }

        #endregion
        // -------------------------------------------------------------------
        #region Connections

        private bool transitionDisconnect = false;

        protected override void StartClient_(Uri connectionUri)
        {

            Debug.Log($"Attempting to connect to {connectionUri}...");

            manager.StartClient(connectionUri);
        }

        protected override async Task StopHost_(bool loadOfflineScene)
        {
            manager.StopHost();
            OpenPorts = false;

            RemotePeerId_ = null;

            if (loadOfflineScene)
                SettingsManager.StartCoroutineAsync(() => TransitionProgressStatic.TransitionTo(null, null));

            // And, wait for the network to really be shut down.
            while (manager.isNetworkActive) await Task.Delay(8);
        }

        protected override async Task StartHost_(bool resetConnection = false)
        {
            if (resetConnection)
                await SmoothServerTransition();

            OpenPorts = true;
            ConnectionManager.ExpectConnectionResponse();

            // Custom server port -- Transport specific!
            transport.port = (ushort) SettingsManager.Server.ServerPort;
            manager.StartHost();

            // And, wait for the network to really be started up.
            while (!manager.isNetworkActive) await Task.Delay(8);
        }

        protected override async Task StartServer_()
        {
            await SmoothServerTransition();

            OpenPorts = true;

            // Custom server port -- Transport specific!
            transport.port = (ushort)SettingsManager.Server.ServerPort;
            manager.StartServer();

            // And, wait for the network to really be started up.
            while (!manager.isNetworkActive) await Task.Delay(8);
        }

        private async Task SmoothServerTransition()
        {
            if (manager.isNetworkActive)
            {
                transitionDisconnect = true;
                await StopHost_(false);
            }
        }

        private void OnRemoteDisconnected()
        {
            // Client to offline. It it's planned to switch servers,
            // skip the offline scene.
            if(!transitionDisconnect) _ = StopHost_(true);
            transitionDisconnect = false;
        }
        #endregion
        // -------------------------------------------------------------------
        #region User Data queries
        protected override IAvatarBrain GetOnlineUser_(UserID userID)
        {
            IEnumerable<IAvatarBrain> q =
                from entry in GameObject.FindGameObjectsWithTag("Player")
                where entry.GetComponent<IAvatarBrain>().UserID == userID
                select entry.GetComponent<IAvatarBrain>();

            return q.Count() > 0 ? q.First() : null;
        }

        protected override IAvatarBrain GetOnlineUser_(uint netId)
        {
            IEnumerable<IAvatarBrain> q =
                from entry in GameObject.FindGameObjectsWithTag("Player")
                where entry.GetComponent<IAvatarBrain>().NetID == netId
                select entry.GetComponent<IAvatarBrain>();

            return q.Count() > 0 ? q.First() : null;
        }

        protected override IEnumerable<IAvatarBrain> GetOnlineUsers_()
        {
            return from entry in GameObject.FindGameObjectsWithTag("Player")
                   select entry.GetComponent<IAvatarBrain>();

        }

        #endregion
        // -------------------------------------------------------------------
    }
}
