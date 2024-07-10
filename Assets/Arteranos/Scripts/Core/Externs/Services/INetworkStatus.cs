/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Avatar;
using Arteranos.Core;
using Ipfs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Arteranos.Services
{
    public interface INetworkStatus : IMonoBehaviour
    {
        bool IsClientConnected_ { get; }
        bool IsClientConnecting_ { get; }
        Action<bool, string> OnClientConnectionResponse_ { get; set; }
        bool OpenPorts_ { get; set; }
        MultiHash RemotePeerId_ { get; set; }

        event Action<ConnectivityLevel, OnlineLevel> OnNetworkStatusChanged_;

        ConnectivityLevel GetConnectivityLevel_();
        OnlineLevel GetOnlineLevel_();
        IEnumerable<IAvatarBrain> GetOnlineUsers_();
        IAvatarBrain GetOnlineUser_(uint netId);
        IAvatarBrain GetOnlineUser_(UserID userID);
        void StartClient_(Uri connectionUri);
        Task StartHost_(bool resetConnection = false);
        Task StartServer_();
        Task StopHost_(bool loadOfflineScene);
    }
}