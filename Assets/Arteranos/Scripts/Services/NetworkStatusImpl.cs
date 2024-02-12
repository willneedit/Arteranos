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

namespace Arteranos.Services
{
    public partial class NetworkStatusImpl : NetworkStatus
    {
        private INatDevice device = null;
        protected override IPAddress ExternalAddress_ { get; set; } = null;

        protected override IPAddress PublicIPAddress_ { get; set; } = null;

        protected override string ServerHost_ { get; set; } = null;

        protected override int ServerPort_ { get; set; } = 0;

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

        void OnEnable()
        {
            Debug.Log("Setting up NAT gateway configuration");

            void FoundPublicIPAddress(IPAddress address)
            {
                PublicIPAddress_ = address;
                Debug.Log($"    Public IP: {PublicIPAddress_?.ToString() ?? "Unknown!"}");
            }

            IEnumerator RefreshDiscovery()
            {
                while(true)
                {
                    // No sense for router and IP detection if the computer's network cable is unplugged
                    // and in its airplane mode.
                    if (GetConnectivityLevel_() == ConnectivityLevel.Unconnected)
                    {
                        PublicIPAddress_ = null;
                        yield return new WaitForSeconds(10);
                    }
                    else
                    {
                        // Needs to be refreshed anytime, because the router invalidates port forwarding
                        // if the connected device falls idle, or catatonic.
                        NatUtility.StartDiscovery();

                        // Only lean on the remote services if we don't have the public IP determined yet.
                        if (PublicIPAddress_ == null) GetMyIP(FoundPublicIPAddress);

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
            // No NAT router at all? Lucky you! ;-)
            if(ExternalAddress_ == null) return;

            // No point to open the ports if we're not supposed to.
            if(!OpenPorts_) return;

            Debug.Log("Opening ports in the router");

            Server ss = SettingsManager.Server;

            ServerPortPublic = await OpenPortAsync(ss.ServerPort);
            MetadataPortPublic = await OpenPortAsync(ss.MetadataPort);
        }

        public void ClosePortsAsync()
        {
            // No NAT router at all? Lucky you! ;-)
            if(ExternalAddress_ == null) return;

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
        #region IP Address determination

        public async void GetMyIP(Action<IPAddress> callback) 
            => callback?.Invoke(await GetMyIpAsync());

        private static CancellationTokenSource s_cts = null;

        public static async Task<IPAddress> GetMyIpAsync()
        {
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

            s_cts = new CancellationTokenSource();

            IPAddress result = null;

            async Task GetOneMyIP(string service)
            {
                if (result != null || s_cts.Token.IsCancellationRequested) return;
                result = await GetMyIPAsync(service);
            }

            TaskPool<string> pool = new(1);

            foreach(string service in services)
                pool.Schedule(service, GetOneMyIP);

            await pool.Run(s_cts.Token);

            return result;
        }


        public static async Task<IPAddress> GetMyIPAsync(string service)
        {
            try
            {
                using HttpClient webclient = new();

                HttpResponseMessage response = await webclient.GetAsync(service, s_cts.Token);
                string ipString = await response.Content.ReadAsStringAsync();

                // https://ihateregex.io/expr/ip
                Match m = Regex.Match(ipString, @"(\b25[0-5]|\b2[0-4][0-9]|\b[01]?[0-9][0-9]?)(\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)){3}");
                
                return m.Success ? IPAddress.Parse(m.Value) : null;
            }
            catch { }
            return null;
        }

        #endregion
        // -------------------------------------------------------------------
        #region Connections

        private bool transitionDisconnect = false;

        protected override void StartClient_(Uri connectionUri)
        {

            Debug.Log($"Attempting to connect to {connectionUri}...");

            manager.StartClient(connectionUri);

            // It's preliminary, sure...
            ServerHost_ = connectionUri.Host;
            ServerPort_ = connectionUri.Port;
        }

        protected override async Task StopHost_(bool loadOfflineScene)
        {
            manager.StopHost();
            OpenPorts = false;

            ServerHost_ = null;

            if (loadOfflineScene)
            {
                XR.ScreenFader.StartFading(1.0f);
                await Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    await WorldTransition.MoveToOfflineWorld();
                });

                XR.ScreenFader.StartFading(0.0f);
            }

            // And, wait for the network to really be shut down.
            while (manager.isNetworkActive) await Task.Yield();
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
            while (!manager.isNetworkActive) await Task.Yield();
        }

        protected override async Task StartServer_()
        {
            await SmoothServerTransition();

            OpenPorts = true;

            // Custom server port -- Transport specific!
            transport.port = (ushort)SettingsManager.Server.ServerPort;
            manager.StartServer();

            // And, wait for the network to really be started up.
            while (!manager.isNetworkActive) await Task.Yield();
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

        // TODO implement ServerUserBase database lookup (see UserPanel_ServerUserList)
        #endregion
        // -------------------------------------------------------------------
    }
}
