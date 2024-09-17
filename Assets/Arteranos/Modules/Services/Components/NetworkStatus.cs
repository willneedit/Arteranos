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

using System.Collections.Generic;
using Arteranos.Avatar;
using System.Linq;
using Ipfs;
using System.Net;

namespace Arteranos.Services
{
    public partial class NetworkStatus : MonoBehaviour, INetworkStatus
    {

        private readonly Dictionary<IPAddress, INatDevice> Devices = new();
        public MultiHash RemotePeerId { get; set; } = null;

        public event Action<ConnectivityLevel, OnlineLevel> OnNetworkStatusChanged;
        public Action<bool, string> OnClientConnectionResponse { get => m_OnClientConnectionResponse; set => m_OnClientConnectionResponse = value; }

        public bool OpenPorts
        {
            get => m_OpenPorts;
            set
            {
                bool oldvalue = m_OpenPorts;
                bool newvalue = value && (G.Server?.UseUPnP ?? false);
                if(oldvalue != newvalue)
                {
                    m_OpenPorts = newvalue;
                    if(m_OpenPorts) OpenPortsAsync();
                    else ClosePortsAsync();
                }
            }
        }

        private Action<bool, string> m_OnClientConnectionResponse = null;

        public bool ServerPortPublic = false;

        private ConnectivityLevel CurrentConnectivityLevel = ConnectivityLevel.Unconnected;
        private OnlineLevel CurrentOnlineLevel = OnlineLevel.Offline;
        private bool m_OpenPorts = false;

        private NetworkManager manager = null;
        private TelepathyTransport transport = null;
        private void Awake()
        {
            manager = FindObjectOfType<NetworkManager>(true);
            transport = FindObjectOfType<TelepathyTransport>(true);

            G.NetworkStatus = this;
        }

        private void OnDestroy()
        {
            ClosePortsAsync();
        }

        // -------------------------------------------------------------------
        #region Running
        public ConnectivityLevel GetConnectivityLevel()
        {
            if(Application.internetReachability == NetworkReachability.NotReachable)
                return ConnectivityLevel.Unconnected;

            return (ServerPortPublic)
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

        public bool IsClientConnecting => NetworkClient.isConnecting;

        public bool IsClientConnected => NetworkClient.isConnected;

        void OnEnable()
        {
            Debug.Log("Setting up NAT gateway configuration");

            IEnumerator RefreshDiscovery()
            {
                while(true)
                {
                    // No sense for router and IP detection if the computer's network cable is unplugged
                    // and in its airplane mode.
                    if (GetConnectivityLevel() == ConnectivityLevel.Unconnected)
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
            ConnectivityLevel c1 = GetConnectivityLevel();
            OnlineLevel c2 = GetOnlineLevel();

            if(CurrentConnectivityLevel != c1 || CurrentOnlineLevel != c2)
                OnNetworkStatusChanged?.Invoke(c1, c2);

            if(CurrentOnlineLevel == OnlineLevel.Client && c2 == OnlineLevel.Offline)
                OnRemoteDisconnected();

            CurrentConnectivityLevel = c1;
            CurrentOnlineLevel = c2;
        }

        #endregion
        // -------------------------------------------------------------------
        #region Connectivity and UPnP

        private bool reportRouter = false;

        private void DeviceFound(object sender, DeviceEventArgs e)
        {
            IPAddress localAddress = null;

            if (e.Device is Mono.Nat.Upnp.UpnpNatDevice uPnPNatDevice)
                localAddress = uPnPNatDevice.LocalAddress;

            if(reportRouter)
            {
                Debug.Log($"Device found : {e.Device.NatProtocol}");
                if (Devices.Count == 0)
                    Debug.Log($"  Extern IP  : {e.Device.GetExternalIP()}");
                Debug.Log($"  Type       : {e.Device.GetType().Name}");

                if (localAddress == null)
                    Debug.Log($"  Local addr : {localAddress}");
            }

            Devices.Add(localAddress, e.Device);
        }

        private async Task<bool> OpenPortAsync(int port)
        {
            // TCP, and internal and external ports as the same.
            Mapping mapping = new(Protocol.Tcp, port, port);
            foreach(var device in Devices)
            {
                try
                {
                    await device.Value.CreatePortMapAsync(mapping);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to create a port mapping for {port}");
                    Debug.LogException(ex);
                    if (device.Value is Mono.Nat.Upnp.UpnpNatDevice usPnPNatDevice)
                        Debug.Log($"Local address: {usPnPNatDevice.LocalAddress}");
                }
            }

            return true;
        }

        private async void ClosePortAsync(int port)
        {
            // TCP, and internal and external ports as the same.
            Mapping mapping = new(Protocol.Tcp, port, port);

            foreach (var device in Devices)
            {
                try
                {
                    await device.Value.DeletePortMapAsync(mapping);
                }
                catch
                {
                    Debug.Log($"Failed to delete a port mapping for {port}... but it's okay.");
                }
            }
        }

        public async void OpenPortsAsync()
        {
            // No point to open the ports if we're not supposed to.
            if(!OpenPorts) return;

            Debug.Log("Opening ports in the router");

            Server ss = G.Server;

            ServerPortPublic = await OpenPortAsync(ss.ServerPort);
        }

        public void ClosePortsAsync()
        {
            Debug.Log("Closing ports in the router, if there's need to do.");

            Server ss = G.Server;

            if (ServerPortPublic) ClosePortAsync(ss.ServerPort);

            ServerPortPublic = false;
        }

        #endregion
        // -------------------------------------------------------------------
        #region Connections

        private bool transitionDisconnect = false;

        public void StartClient(Uri connectionUri)
        {

            Debug.Log($"Attempting to connect to {connectionUri}...");

            manager.StartClient(connectionUri);
        }

        public async Task StopHost(bool loadOfflineScene)
        {
            manager.StopHost();
            OpenPorts = false;

            RemotePeerId = null;

            if (loadOfflineScene)
                Core.TaskScheduler.ScheduleCoroutine(() => TransitionProgress.TransitionTo(null));

            // And, wait for the network to really be shut down.
            while (manager.isNetworkActive) await Task.Delay(8);
        }

        public async Task StartHost(bool resetConnection = false)
        {
            if (resetConnection)
                await SmoothServerTransition();

            OpenPorts = true;
            G.ConnectionManager.ExpectConnectionResponse();

            // Custom server port -- Transport specific!
            transport.port = (ushort) G.Server.ServerPort;
            manager.StartHost();

            // And, wait for the network to really be started up.
            while (!manager.isNetworkActive) await Task.Delay(8);
        }

        public async Task StartServer()
        {
            await SmoothServerTransition();

            OpenPorts = true;

            // Custom server port -- Transport specific!
            transport.port = (ushort)G.Server.ServerPort;
            manager.StartServer();

            // And, wait for the network to really be started up.
            while (!manager.isNetworkActive) await Task.Delay(8);
        }

        private async Task SmoothServerTransition()
        {
            if (manager.isNetworkActive)
            {
                transitionDisconnect = true;
                await StopHost(false);
            }
        }

        private void OnRemoteDisconnected()
        {
            // Client to offline. It it's planned to switch servers,
            // skip the offline scene.
            if(!transitionDisconnect) _ = StopHost(true);
            transitionDisconnect = false;
        }
        #endregion
        // -------------------------------------------------------------------
        #region User Data queries
        public IAvatarBrain GetOnlineUser(UserID userID)
        {
            IEnumerable<IAvatarBrain> q =
                from entry in GameObject.FindGameObjectsWithTag("Player")
                where entry.GetComponent<IAvatarBrain>().UserID == userID
                select entry.GetComponent<IAvatarBrain>();

            return q.Count() > 0 ? q.First() : null;
        }

        public IAvatarBrain GetOnlineUser(uint netId)
        {
            IEnumerable<IAvatarBrain> q =
                from entry in GameObject.FindGameObjectsWithTag("Player")
                where entry.GetComponent<IAvatarBrain>().NetID == netId
                select entry.GetComponent<IAvatarBrain>();

            return q.Count() > 0 ? q.First() : null;
        }

        public IEnumerable<IAvatarBrain> GetOnlineUsers()
        {
            return from entry in GameObject.FindGameObjectsWithTag("Player")
                   select entry.GetComponent<IAvatarBrain>();

        }

        #endregion
        // -------------------------------------------------------------------
    }
}
