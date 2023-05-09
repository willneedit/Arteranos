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

namespace Arteranos
{
    public class NetworkStatus : MonoBehaviour
    {
        private INatDevice device = null;
        public System.Net.IPAddress ExternalAddress = null;

        public bool ServerPortPublic = false;
        public bool VoicePortPublic = false;
        public bool MetadataPortPublic = false;

        public bool? IsPublic()
        {
            // Clear-cut case.
            if(Application.internetReachability == NetworkReachability.NotReachable) return false;

            // It could be a network that has a firewall that cannot
            // be talked to. Or, an airgapped network.
            if(ExternalAddress == null) return null;

            // NAT in place, all that counts is if the ports open.
            return ServerPortPublic && VoicePortPublic && MetadataPortPublic;
        }

        void Start()
        {
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

        private async void DeviceFound(object sender, DeviceEventArgs e)
        {
            // For some reason, my Fritz!Box reponds twice, for two WAN ports,
            // all with the same external IP address?
            if(device != null) return;

            device = e.Device;

            ExternalAddress = await device.GetExternalIPAsync();

            Debug.Log($"Device found : {device}");
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

        private void OnDestroy()
        {
            Debug.Log("Shutting down NAT gateway configuration");

            ClosePortsAsync();

            NatUtility.StopDiscovery();
        }
    }
}
