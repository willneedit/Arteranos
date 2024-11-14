/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using Ipfs;
using System;
using System.Collections;
using UnityEngine;

namespace Arteranos.Services
{
    public interface IConnectionManager : IMonoBehaviour
    {
        IEnumerator ConnectToServer(MultiHash PeerID, Action<bool> callback);
        void DeliverDisconnectReason(string reason);
        void ExpectConnectionResponse();
        void Peer_InitateNatPunch(NatPunchRequestData nprd);
    }
}