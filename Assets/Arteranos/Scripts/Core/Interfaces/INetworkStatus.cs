/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.ComponentModel;
using System.Net;

#pragma warning disable IDE1006 // Because Unity's more relaxed naming convention

namespace Arteranos.Services
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
    public interface INetworkStatus
    {
        IPAddress ExternalAddress { get; }
        bool OpenPorts { get; set; }
        bool enabled { get; set; }
        Action<bool, string> OnClientConnectionResponse { get; set; }

        event Action<ConnectivityLevel, OnlineLevel> OnNetworkStatusChanged;

        ConnectivityLevel GetConnectivityLevel();
        OnlineLevel GetOnlineLevel();
        void StartClient(Uri connectionUri);
        void StartHost();
        void StartServer();
        void StopHost(bool loadOfflineScene);
    }
}
