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
using System.ComponentModel;

namespace Arteranos.Services
{
    public class NetworkStatus : MonoBehaviour
    {
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


        private INatDevice device = null;
        public System.Net.IPAddress ExternalAddress { get; internal set; } = null;

        public static event Action<ConnectivityLevel, OnlineLevel> OnNetworkStatusChanged;

        public bool ServerPortPublic = false;
        public bool VoicePortPublic = false;
        public bool MetadataPortPublic = false;

        private ConnectivityLevel CurrentConnectivityLevel = ConnectivityLevel.Unconnected;
        private OnlineLevel CurrentOnlineLevel = OnlineLevel.Offline;

        public static ConnectivityLevel GetConnectivityLevel()
        {
            NetworkStatus ns = FindObjectOfType<NetworkStatus>();

            if(Application.internetReachability == NetworkReachability.NotReachable)
                return ConnectivityLevel.Unconnected;

            return (ns.ServerPortPublic && ns.VoicePortPublic && ns.MetadataPortPublic)
                ? ConnectivityLevel.Unrestricted
                : ConnectivityLevel.Restricted;
        }

        public static OnlineLevel GetOnlineLevel()
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
            catch (Exception ex)
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
            catch(Exception ex)
            {
                Debug.LogWarning($"Failed to delete a port mapping for {port}");
                Debug.LogException(ex);
            }
        }

        public async void OpenPortsAsync()
        {
            // No NAT router at all? Lucky you! ;-)
            if (ExternalAddress == null) return;

            ServerSettings ss = SettingsManager.Server;

            ServerPortPublic = await OpenPortAsync(ss.ServerPort);
            VoicePortPublic = await OpenPortAsync(ss.VoicePort);
            MetadataPortPublic = await OpenPortAsync(ss.MetadataPort);
        }

        public void ClosePortsAsync()
        {
            // No NAT router at all? Lucky you! ;-)
            if(ExternalAddress == null) return;

            ServerSettings ss = SettingsManager.Server;

            if(ServerPortPublic)
                ClosePortAsync(ss.ServerPort);

            if(VoicePortPublic)
                ClosePortAsync(ss.VoicePort);
            
            if(MetadataPortPublic)
                ClosePortAsync(ss.MetadataPort);
        }
    }
}
