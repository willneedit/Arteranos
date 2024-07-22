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
        Cid CurrentSDCid { get; }
        Cid IdentifyCid { get; }
        IpfsClientEx Ipfs { get; }
        Peer Self { get; }
        SignKey ServerKeyPair { get; }
        bool UsingPubsub { get; }

        void DownloadServerOnlineData(MultiHash SenderPeerID, Action callback = null);
        Task FlipServerDescription(bool reload);
        Task<IPAddress> GetPeerIPAddress(MultiHash PeerID, CancellationToken token = default);
        Task PinCid(Cid cid, bool pinned, CancellationToken token = default);
        Task<byte[]> ReadBinary(string path, Action<long> reportProgress = null, CancellationToken cancel = default);
    }
}