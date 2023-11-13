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
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using System.Threading;
using System.Text.RegularExpressions;

namespace Arteranos.Services
{
    public partial class NetworkStatusImpl : MonoBehaviour, INetworkStatus
    {
        private INatDevice device = null;
        public IPAddress ExternalAddress { get; internal set; } = null;

        public IPAddress PublicIPAddress { get; internal set; } = null;

        public string ServerHost { get; internal set; } = null;

        public int ServerPort { get; internal set; } = 0;

        public event Action<ConnectivityLevel, OnlineLevel> OnNetworkStatusChanged;

        public Action<bool, string> OnClientConnectionResponse { get => m_OnClientConnectionResponse; set => m_OnClientConnectionResponse = value; }

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

        private Action<bool, string> m_OnClientConnectionResponse = null;

        public bool ServerPortPublic = false;
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

        // -------------------------------------------------------------------
        #region Running
        public ConnectivityLevel GetConnectivityLevel()
        {
            if(Application.internetReachability == NetworkReachability.NotReachable)
                return ConnectivityLevel.Unconnected;

            return (ServerPortPublic && MetadataPortPublic)
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

            void FoundPublicIPAddress(IPAddress address)
            {
                PublicIPAddress = address;
                Debug.Log($"    Public IP: {PublicIPAddress}");
            }

            IEnumerator RefreshDiscovery()
            {
                while(true)
                {
                    // No sense for router and IP detection if the computer's network cable is unplugged
                    // and in its airplane mode.
                    if (GetConnectivityLevel() == ConnectivityLevel.Unconnected)
                    {
                        PublicIPAddress = null;
                        yield return new WaitForSeconds(10);
                    }
                    else
                    {
                        // Needs to be refreshed anytime, because the router invalidates port forwarding
                        // if the connected device falls idle, or catatonic.
                        NatUtility.StartDiscovery();

                        // Only lean on the remote services if we don't have the public IP determined yet.
                        if (PublicIPAddress == null) GetMyIP(FoundPublicIPAddress);

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

            Server ss = SettingsManager.Server;

            ServerPortPublic = await OpenPortAsync(ss.ServerPort);
            MetadataPortPublic = await OpenPortAsync(ss.MetadataPort);
        }

        public void ClosePortsAsync()
        {
            // No NAT router at all? Lucky you! ;-)
            if(ExternalAddress == null) return;

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

            int index = 0;
            List<Task<IPAddress>> runners = services.Select(service => GetMyIPAsync(service, index++)).ToList();

            // winner takes all ...
            Task<IPAddress> winner = await Task.WhenAny(runners);
            IPAddress result = await winner;

            // ... losers get nothing.
            s_cts.Cancel();

            return result;
        }

        public static async Task<IPAddress> GetMyIPAsync(string service, int index)
        {
            try
            {
                await Task.Delay(index * 250, s_cts.Token);
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
        public void StartClient(Uri connectionUri)
        {
            NetworkManager manager = FindObjectOfType<NetworkManager>();

            Debug.Log($"Attempting to connect to {connectionUri}...");

            manager.StartClient(connectionUri);

            // It's preliminary, sure...
            ServerHost = connectionUri.Host;
            ServerPort = connectionUri.Port;
        }

        public void StopHost(bool loadOfflineScene)
        {
            static IEnumerator StopHostCoroutine(bool loadOfflineScene)
            {
                if (loadOfflineScene)
                {
                    XR.ScreenFader.StartFading(1.0f);

                    yield return new WaitForSeconds(1.0f);
                    AsyncOperation ao = SceneManager.LoadSceneAsync("OfflineScene");

                    if (!ao.isDone)
                        yield return new WaitForEndOfFrame();
                }

                NetworkManager manager = FindObjectOfType<NetworkManager>();
                manager.StopHost();
                NetworkStatus.OpenPorts = false;

                if (loadOfflineScene)
                    WorldDownloaderLow.MoveToDownloadedWorld();
            }

            ServerHost = null;
            StartCoroutine(StopHostCoroutine(loadOfflineScene));
        }

        public void StartHost()
        {
            NetworkManager manager = FindObjectOfType<NetworkManager>();

            NetworkStatus.OpenPorts = true;
            
            ConnectionManager.Instance.ExpectConnectionResponse();

            // Custom server port -- Transport specific!
            FindObjectOfType<TelepathyTransport>().port = (ushort) SettingsManager.Server.ServerPort;
            manager.StartHost();
        }

        public async void StartServer()
        {
            NetworkManager manager = FindObjectOfType<NetworkManager>();

            if(manager.isNetworkActive)
            {
                manager.StopHost();

                await Task.Delay(1000);
            }

            NetworkStatus.OpenPorts = true;

            // Custom server port -- Transport specific!
            FindObjectOfType<TelepathyTransport>().port = (ushort)SettingsManager.Server.ServerPort;
            manager.StartServer();
        }

        public void OnRemoteDisconnected()
        {
            // Client to offline.
            StopHost(true);
        }
        #endregion
        // -------------------------------------------------------------------
    }
}
