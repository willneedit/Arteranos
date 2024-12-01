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
using System.Threading;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Net.NetworkInformation;

namespace Arteranos.Services
{
    public partial class NetworkStatus : MonoBehaviour, INetworkStatus
    {
        public AsyncLazy<List<IPAddress>> IPAddresses => m_IPAddresses;

        private INatDevice device = null;
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
        private Transport transport = null;
        private void Awake()
        {
            manager = FindObjectOfType<NetworkManager>(true);
            transport = FindObjectOfType<Transport>(true);

            G.NetworkStatus = this;
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
            bool clientConnected = NetworkClient.active && NetworkClient.isConnected;

            if (!clientConnected && !NetworkServer.active)
                return OnlineLevel.Offline;

            if(clientConnected && NetworkServer.active)
                return OnlineLevel.Host;

            return clientConnected
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
                RefreshIPAddresses();

                yield return IPAddresses.WaitFor();

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

        private void DeviceFound(object sender, DeviceEventArgs e)
        {
            // For some reason, my Fritz!Box reponds twice, for two WAN ports,
            // all with the same external IP address?
            if(device != null) return;

            device = e.Device;

            // ExternalAddress_ = await device.GetExternalIPAsync();

            Debug.Log($"Device found : {device.NatProtocol}");
            Debug.Log($"  Type       : {device.GetType().Name}");

            OpenPortsAsync();
        }

        private async Task<bool> OpenPortAsync(int port)
        {
            // UDP, and internal and external ports as the same.
            Mapping mapping = new(Protocol.Udp, port, port);

            try
            {
                await device.CreatePortMapAsync(mapping);
                return true;
            }
            catch(Exception ex)
            {
                Debug.Log($"Failed to create a port mapping for {port}: {ex.Message}");
                return false;
            }
        }

        private async void ClosePortAsync(int port)
        {
            // UDP, and internal and external ports as the same.
            Mapping mapping = new(Protocol.Udp, port, port);

            try
            {
                await device.DeletePortMapAsync(mapping);
            }
            catch
            {
                Debug.Log($"Failed to delete a port mapping for {port}... but that's okay.");
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

            if(ServerPortPublic)
                ClosePortAsync(ss.ServerPort);

            ServerPortPublic = false;
        }

        #endregion
        // -------------------------------------------------------------------
        #region IP Address determination

        private AsyncLazy<List<IPAddress>> m_IPAddresses = null;

        private void RefreshIPAddresses()
        {
            m_IPAddresses = new(async () => await GatherIPAddresses());
        }

        private async Task<List<IPAddress>> GatherIPAddresses()
        {
            List<IPAddress> ips = new();

            IPAddress externalIPAddress = null;
            for(int i = 0; i < 10; i++)
            {
                externalIPAddress = await GetExternalIPAdress();
                if (externalIPAddress != null) break;
            }

            if(externalIPAddress != null) ips.Add(externalIPAddress);

            ips.AddRange(GetLocalIPAddresses());

            foreach (IPAddress ip in ips)
                Debug.Log($" Found IP address: {ip}");

            return ips;
        }

        private static List<IPAddress> GetLocalIPAddresses()
        {
            List<IPAddress> addr = new();

            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in adapters)
            {
                IPInterfaceProperties adapterProperties = adapter.GetIPProperties();
                UnicastIPAddressInformationCollection uniCast = adapterProperties.UnicastAddresses;
                if (uniCast.Count > 0)
                    foreach (UnicastIPAddressInformation uni in uniCast)
                    {
                        if (uni.Address.IsIPv6LinkLocal
                            || uni.PrefixOrigin == PrefixOrigin.WellKnown) continue;

                        addr.Add(uni.Address);
                    }
            }

            return addr;
        }

        public static async Task<IPAddress> GetExternalIPAdress()
        {
            CancellationTokenSource cts = null;
            IPAddress result = null;
            using HttpClient webclient = new();

            async Task<IPAddress> GetMyIPAsync(string service, CancellationToken cancel)
            {
                try
                {
                    HttpResponseMessage response = await webclient.GetAsync(service, cancel);
                    string ipString = await response.Content.ReadAsStringAsync();

                    // https://ihateregex.io/expr/ip
                    Match m = Regex.Match(ipString, @"(\b25[0-5]|\b2[0-4][0-9]|\b[01]?[0-9][0-9]?)(\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)){3}");

                    return m.Success ? IPAddress.Parse(m.Value) : null;
                }
                catch { }
                return null;
            }

            
            // Services from https://stackoverflow.com/questions/3253701/get-public-external-ip-address
            List<string> services = new()
            {
                "https://ipv4.icanhazip.com",
                "https://api.ipify.org",
                "https://ipinfo.io/ip",
                "https://checkip.amazonaws.com",
                "https://wtfismyip.com/text",
                "http://icanhazip.com"
            };

            // Spread the load throughout on all of the services.
            services.Shuffle();

            cts = new CancellationTokenSource(2000);

            async Task GetOneMyIP(string service)
            {
                if (result != null || cts.Token.IsCancellationRequested) return;
                result = await GetMyIPAsync(service, cts.Token);

                if (result != null) cts.Cancel();
            }

            TaskPool<string> pool = new(2);

            foreach (string service in services)
                pool.Schedule(service, GetOneMyIP);

            await pool.Run(cts.Token);

            return result;
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

            SetPort();
            manager.StartHost();

            // And, wait for the network to really be started up.
            while (!manager.isNetworkActive) await Task.Delay(8);
        }

        private void SetPort()
        {
            // Set the port number on supported transports, as long as they'd
            // require it.
            if (transport is TelepathyTransport tt)
                tt.port = (ushort)G.Server.ServerPort;
            else if (transport is LiteNetLibTransport lnlt)
                lnlt.port = (ushort)G.Server.ServerPort;
        }

        public async Task StartServer()
        {
            await SmoothServerTransition();

            OpenPorts = true;

            SetPort();
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
