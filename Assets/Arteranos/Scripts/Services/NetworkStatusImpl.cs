/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Mono.Nat;
using System;
using System.Threading.Tasks;
using Arteranos.Core;
using Mirror;
using System.Net;

namespace Arteranos.Services
{
    public partial class NetworkStatusImpl : MonoBehaviour, INetworkStatus
    {
        private INatDevice device = null;
        public IPAddress ExternalAddress { get; internal set; } = null;

        public event Action<ConnectivityLevel, OnlineLevel> OnNetworkStatusChanged;

        public Action<bool> OnClientConnectionResponse { get => m_OnClientConnectionResponse; set => m_OnClientConnectionResponse = value; }


        public new bool enabled
        {
            get => base.enabled;
            set => base.enabled = value;
        }

        public bool OpenPorts
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

        private Action<bool> m_OnClientConnectionResponse = null;

        public bool ServerPortPublic = false;
        public bool VoicePortPublic = false;
        public bool MetadataPortPublic = false;

        private ConnectivityLevel CurrentConnectivityLevel = ConnectivityLevel.Unconnected;
        private OnlineLevel CurrentOnlineLevel = OnlineLevel.Offline;
#if UNITY_SERVER
        // NetworkManager sets up the server by default, so do that the ports, too.
        private bool m_OpenPorts = true;
#else
        private bool m_OpenPorts = false;
#endif

        private void Awake() => NetworkStatus.Instance = this;
        private void OnDestroy()
        {
            ClosePortsAsync();
            NetworkStatus.Instance = null;
        }

        #region Connectivity and UPnP
        public ConnectivityLevel GetConnectivityLevel()
        {
            if(Application.internetReachability == NetworkReachability.NotReachable)
                return ConnectivityLevel.Unconnected;

            return (ServerPortPublic && VoicePortPublic && MetadataPortPublic)
                ? ConnectivityLevel.Unrestricted
                : ConnectivityLevel.Restricted;
        }

        public OnlineLevel GetOnlineLevel()
        {
            if(!NetworkClient.active && !NetworkServer.active)
                return OnlineLevel.Offline;

            if(NetworkClient.active && NetworkServer.active)
                return OnlineLevel.Host;

            return NetworkClient.active
                ? OnlineLevel.Client
                : OnlineLevel.Server;
        }

        void OnEnable()
        {
            Debug.Log("Setting up NAT gateway configuration");

            static IEnumerator RefreshDiscovery()
            {
                while(true)
                {
                    yield return new WaitForSeconds(500);

                    NatUtility.StopDiscovery();
                    NatUtility.StartDiscovery();
                }
            }

            NatUtility.DeviceFound += DeviceFound;

            // Look for any device, any protocol.
            NatUtility.StartDiscovery();

            StartCoroutine(RefreshDiscovery());
        }

        private void OnDisable()
        {
            Debug.Log("Shutting down NAT gateway configuration");

            StopAllCoroutines();

            ClosePortsAsync();

            NatUtility.StopDiscovery();
        }

        private void Update()
        {
            ConnectivityLevel c1 = GetConnectivityLevel();
            OnlineLevel c2 = GetOnlineLevel();

            if(CurrentConnectivityLevel != c1 || CurrentOnlineLevel != c2)
                OnNetworkStatusChanged?.Invoke(c1, c2);

            CurrentConnectivityLevel = c1;
            CurrentOnlineLevel = c2;
        }

        private async void DeviceFound(object sender, DeviceEventArgs e)
        {
            // For some reason, my Fritz!Box reponds twice, for two WAN ports,
            // all with the same external IP address?
            if(device != null) return;

            device = e.Device;

            ExternalAddress = await device.GetExternalIPAsync();

            Debug.Log($"Device found : {device.NatProtocol}");
            Debug.Log($"  Type       : {device.GetType().Name}");
            Debug.Log($"  External IP: {ExternalAddress}");

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
            // No NAT router at all? Lucky you! ;-)
            if(ExternalAddress == null) return;

            // No point to open the ports if we're not supposed to.
            if(!OpenPorts) return;

            Debug.Log("Opening ports in the router");

            ServerSettings ss = SettingsManager.Server;

            ServerPortPublic = await OpenPortAsync(ss.ServerPort);
            VoicePortPublic = await OpenPortAsync(ss.VoicePort);
            MetadataPortPublic = await OpenPortAsync(ss.MetadataPort);
        }

        public void ClosePortsAsync()
        {
            // No NAT router at all? Lucky you! ;-)
            if(ExternalAddress == null) return;

            Debug.Log("Closing ports in the router, if there's need to do.");

            ServerSettings ss = SettingsManager.Server;

            if(ServerPortPublic)
                ClosePortAsync(ss.ServerPort);

            if(VoicePortPublic)
                ClosePortAsync(ss.VoicePort);

            if(MetadataPortPublic)
                ClosePortAsync(ss.MetadataPort);

            ServerPortPublic = false;
            VoicePortPublic = false;
            MetadataPortPublic = false;
        }
#endregion

#region Connections
        public void StartClient(Uri connectionUri)
        {
            NetworkManager manager = GameObject.FindObjectOfType<NetworkManager>();

            Debug.Log($"Attempting to connect to {connectionUri}...");

            manager.StartClient(connectionUri);
        }

        public void StopHost()
        {
            NetworkManager manager = GameObject.FindObjectOfType<NetworkManager>();

            manager.StopHost();
            Services.NetworkStatus.OpenPorts = false;
        }

        public void StartHost()
        {
            NetworkManager manager = GameObject.FindObjectOfType<NetworkManager>();

            Services.NetworkStatus.OpenPorts = true;
            manager.StartHost();
        }

        public async void StartServer()
        {
            NetworkManager manager = GameObject.FindObjectOfType<NetworkManager>();

            if(manager.isNetworkActive)
            {
                manager.StopHost();

                await Task.Delay(1009);
            }

            Services.NetworkStatus.OpenPorts = true;
            manager.StartServer();
        }
#endregion
    }
}
