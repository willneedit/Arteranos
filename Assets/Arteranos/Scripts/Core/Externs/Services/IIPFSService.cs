/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core.Cryptography;
using Ipfs;
using Ipfs.Http;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Arteranos.Services
{
    public interface IIPFSService : IMonoBehaviour
    {
        Cid CurrentSDCid_ { get; }
        Cid IdentifyCid_ { get; }
        IpfsClientEx Ipfs_ { get; }
        Peer Self_ { get; }
        SignKey ServerKeyPair_ { get; }
        bool UsingPubsub_ { get; }

        void DownloadServerOnlineData_(MultiHash SenderPeerID, Action callback = null);
        Task FlipServerDescription_(bool reload);
        Task<IPAddress> GetPeerIPAddress_(MultiHash PeerID, CancellationToken token = default);
        Task PinCid_(Cid cid, bool pinned, CancellationToken token = default);
        Task<byte[]> ReadBinary_(string path, Action<long> reportProgress = null, CancellationToken cancel = default);
    }
}