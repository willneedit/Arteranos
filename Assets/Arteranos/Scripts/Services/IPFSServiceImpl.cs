/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Ipfs;
using System.IO;
using Arteranos.Core;
using System.Threading;
using System;
using System.Threading.Tasks;
using Arteranos.Core.Cryptography;
using System.Linq;
using Ipfs.Cryptography.Proto;
using Ipfs.Http;
using System.Net;
using System.Text;
using System.Collections.Concurrent;

namespace Arteranos.Services
{
    public class IPFSServiceImpl : IPFSService
    {
        public override IpfsClientEx Ipfs_ { get => ipfs; }
        public override Peer Self_ { get => self; }
        public override SignKey ServerKeyPair_ { get => serverKeyPair; }
        public override Cid IdentifyCid_ { get; protected set; }
        public override Cid CurrentSDCid_ { get; protected set; } = null;

        public static string CachedPTOSNotice { get; private set; } = null;

        public override event Action<IPublishedMessage> OnReceivedHello_;
        public override event Action<IPublishedMessage> OnReceivedServerDirectMessage_;

        private const string PATH_USER_PRIVACY_NOTICE = "Privacy_TOS_Notice.md";

        private const int heartbeatSeconds = 60;

        private IpfsClientEx ipfs = null;
        private Peer self = null;
        private SignKey serverKeyPair = null;

        private DateTime last = DateTime.MinValue;

        private string versionString = null;

        ServerDescription ServerDescription = null;

        private CancellationTokenSource cts = null;

        private List<byte[]> UserFingerprints = new();

        private ConcurrentDictionary<MultiHash, Peer> DiscoveredPeers = null;

        // ---------------------------------------------------------------
        #region Start & Stop
        private void Start()
        {
            IEnumerator InitializeIPFSCoroutine()
            {
                // Keep the IPFS synced - it needs the IPFS node alive.
                yield return Utils.Async2Coroutine(InitializeIPFS());

                // Initiate the Arteranos peer discovery.
                StartCoroutine(DiscoverPeersCoroutine());

                // Start to emit the server description.
                StartCoroutine(EmitServerDescriptionCoroutine());

                // Start to emit the server online data.
                // TODO Implement key generation first
                // StartCoroutine(EmitServerOnlineDataCoroutine());
            }

            async Task InitializeIPFS()
            {
                // TODO Daemon Startup
                IpfsClientEx ipfsTmp;

                ipfs = null;
                ipfsTmp = new();

                try
                {
                    self = await ipfsTmp.IdAsync();
                    Debug.Log($"IPFS Node's ID: {self.Id}");
                }
                catch
                {
                    Debug.LogError($"Cannot create client RPC");
                    SettingsManager.Quit();
                    return;
                }

                PrivateKey pk = ipfsTmp.ReadDaemonPrivateKey();

                try
                {
                    await ipfsTmp.VerifyDaemonAsync(pk);
                }
                catch (InvalidDataException)
                {
                    Debug.LogError("Daemon doesn't match with its supposed private key");
                    SettingsManager.Quit();
                    return;
                }
                catch
                {
                    Debug.LogError("Daemon communication");
                    SettingsManager.Quit();
                    return;
                }

                // If it doesn't exist, write down the template in the config directory.
                if (!FileUtils.ReadConfig(PATH_USER_PRIVACY_NOTICE, File.Exists))
                {
                    FileUtils.WriteTextConfig(PATH_USER_PRIVACY_NOTICE, SettingsManager.DefaultTOStext);
                    Debug.LogWarning("Privacy notice and Terms Of Service template written down - Read (and modify) according to your use case!");
                }

                CachedPTOSNotice = FileUtils.ReadTextConfig(PATH_USER_PRIVACY_NOTICE);

                try
                {
                    versionString = Core.Version.Load().MMP;
                }
                catch(Exception ex)
                {
                    Debug.LogError("Internal error: Missing version information - use Arteeranos->Build->Update version");
                    Debug.LogException(ex);
                }

                // Put up the identifier file
                StringBuilder sb = new();
                sb.Append("Arteranos Server, built by willneedit\n");
                sb.Append(Core.Version.VERSION_MIN);

                IdentifyCid_ = (await ipfsTmp.FileSystem.AddTextAsync(sb.ToString())).Id;

                ipfs = ipfsTmp;

                // Reuse the IPFS peer key for the multiplayer server to ensure its association
                serverKeyPair = SignKey.ImportPrivateKey(pk);
                SettingsManager.Server.UpdateServerKey(serverKeyPair);


#if false

                await ipfsTmp.PubSub.SubscribeAsync(topic_hello,
                    async msg =>
                    {
                        if (msg.Sender.Id == self.Id) return;
                        bool success = await ParseIncomingIPFSMessageAsync(msg);
                        if (success) OnReceivedHello_?.Invoke(msg);
                    },
                    cts.Token);

                await ipfsTmp.PubSub.SubscribeAsync($"{topic_sdm}/{self.Id}",
                    async msg =>
                    {
                        if (msg.Sender.Id == self.Id) return;
                        bool success = await ParseIncomingIPFSMessageAsync(msg);
                        if (success) OnReceivedServerDirectMessage_?.Invoke(msg);
                    },
                    cts.Token);

                KeyChain kc = await ipfsTmp.KeyChainAsync();
                var kcp = await kc.GetPrivateKeyAsync("self");
                serverKeyPair = SignKey.ImportPrivateKey(kcp);

                ipfs = ipfsTmp;

                // Call back to update the server core data
                SettingsManager.Server.UpdateServerKey(serverKeyPair);

#endif
            }

            Instance = this;
            IdentifyCid_ = null;
            last = DateTime.MinValue;
            DiscoveredPeers = new();
            cts = new();

            UserFingerprints = new List<byte[]>();

            StartCoroutine(GetUserListCoroutine());

            StartCoroutine(InitializeIPFSCoroutine());

        }

        private async void OnDisable()
        {
            await FlipServerDescription_(false);

            Debug.Log("Shutting down the IPFS node.");

            // TODO Daemon Shutdown
            // await ipfs.ShutdownAsync();

            cts?.Cancel();

            cts?.Dispose();

            Instance = null;
        }

        private IEnumerator GetUserListCoroutine()
        {

            while (true)
            {
                UserFingerprints = (from user in NetworkStatus.GetOnlineUsers()
                                             where user.UserPrivacy != null && user.UserPrivacy.Visibility != Visibility.Invisible
                                             select CryptoHelpers.GetFingerprint(user.UserID)).ToList();

                yield return new WaitForSeconds(heartbeatSeconds / 2);

                // yield return Utils.Async2Coroutine(ipfs.Dht.ProvideAsync(IdentifyCid_, true));
            }
            // NOTREACHED
        }

        private IEnumerator DiscoverPeersCoroutine()
        {
            yield break;
            Debug.Log($"Starting node discovery: Identifier file's CID is {IdentifyCid}");

            while(true)
            {
                Task<IEnumerable<Peer>> taskPeers = ipfs.Routing.FindProvidersAsync(IdentifyCid, 
                    1000, // FIXME Maybe configurable.
                    _peer => _ = OnDiscoveredPeer(_peer));

                yield return Utils.Async2Coroutine(taskPeers);

                yield return new WaitForSeconds(heartbeatSeconds * 2);
            }
            // NOTREACHED
        }

        public override async Task<IPAddress> GetPeerIPAddress_(MultiHash PeerID, CancellationToken token = default)
        {
            // Never seen before? Curious.
            // Or too late for being online for the initial FindProvidersAsync()?
            if (!DiscoveredPeers.TryGetValue(PeerID, out Peer found) || !found.Addresses.Any())
                found = await ipfs.Dht.FindPeerAsync(PeerID, token).ConfigureAwait(false);

            // Familiar, but disconnected?
            if (found.ConnectedAddress == null)
                await ipfs.Swarm.ConnectAsync(found.Addresses.First(), token);

            return ParseMultiAddress(found.ConnectedAddress).Item1;
        }

        #endregion
        // ---------------------------------------------------------------
        #region Peer communication and data exchange

        public IEnumerator EmitServerDescriptionCoroutine()
        {
            while(true)
            {
                yield return Utils.Async2Coroutine(FlipServerDescription(true));

                // One day
                yield return new WaitForSeconds(24 * 60 * 60);
            }
        }

        private IEnumerator EmitServerOnlineDataCoroutine()
        {
            while (true)
            {
                if (NetworkStatus.GetOnlineLevel() == OnlineLevel.Server ||
                    NetworkStatus.GetOnlineLevel() == OnlineLevel.Host)
                {
                    yield return Utils.Async2Coroutine(FlipServerOnlineData_());
                }

                yield return new WaitForSeconds(heartbeatSeconds);
            }
            // NOTREACHED
        }

        public override async Task FlipServerDescription_(bool reload)
        {
            if (CurrentSDCid_ != null)
                _ = Ipfs_.Pin.RemoveAsync(CurrentSDCid_);

            if (!reload) return;

            Server server = SettingsManager.Server;
            IEnumerable<string> q = from entry in SettingsManager.ServerUsers.Base
                                    where UserState.IsSAdmin(entry.userState)
                                    select ((string)entry.userID);

            ServerDescription = new()
            {
                Name = server.Name,
                ServerPort = server.ServerPort,
                MetadataPort = server.MetadataPort,
                Description = server.Description,
                ServerIcon = server.ServerIcon,
                Version = versionString,
                MinVersion = Core.Version.VERSION_MIN,
                Permissions = server.Permissions,
                PrivacyTOSNotice = CachedPTOSNotice,
                AdminNames = q.ToArray(),
                PeerID = self.Id.ToString(),
                LastModified = server.ConfigLastChanged,
                ServerOnlineDataLinkCid = null, // TODO key_sod's key CIO
                ServerDescriptionCid = null // Self-reference will be generated AFTER putting it to IPFS
            };

            using MemoryStream ms = new();
            ServerDescription.Serialize(serverKeyPair, ms);
            ms.Position = 0;
            var fsn = await ipfs.FileSystem.AddAsync(ms, "ServerDescription");
            CurrentSDCid_ = fsn.Id;

            // Lasting for two day max, cache refresh needs one minute
            // TODO: NameEx needs work!
            await ipfs.NameEx.PublishAsync(
                CurrentSDCid,
                lifetime: new TimeSpan(2, 0, 0, 0),
                ttl: new TimeSpan(0, 0, 1, 0));

            Debug.Log($"New server description CID: {CurrentSDCid_}");
        }

        private async Task FlipServerOnlineData_()
        {
            // Flood mitigation
            if (last > DateTime.Now - TimeSpan.FromSeconds(30)) return;
            last = DateTime.Now;

            ServerOnlineData sod = new()
            {
                CurrentWorldCid = SettingsManager.WorldCid,
                CurrentWorldName = SettingsManager.WorldName,
                ServerDescriptionCid = CurrentSDCid_,
                UserFingerprints = UserFingerprints,
                LastOnline = last,
                OnlineLevel = NetworkStatus.GetOnlineLevel()
            };

            using MemoryStream ms = new();

            sod.Serialize(ms);
            ms.Position = 0;
            var fsn = await ipfs.FileSystem.AddAsync(ms, "ServerOnlineData");

            // Lasting for five minutes, ttl 30s, under the secondary key
            await ipfs.NameEx.PublishAsync(
                CurrentSDCid,
                key: "key_sod",
                lifetime: new TimeSpan(0, 5, 0),
                ttl: new TimeSpan(0, 0, 30));
        }

        private async Task<bool> ParseServerOnlineData(ServerOnlineData sod, Peer SenderPeer)
        {
            async Task UpdateSD(ServerOnlineData sod, string SenderPeerID)
            {
                using CancellationTokenSource cts = new(500);

                Stream s = await ipfs.FileSystem.ReadFileAsync(sod.ServerDescriptionCid, cts.Token);

                PublicKey pk = PublicKey.FromId(SenderPeerID);
                ServerDescription sd = ServerDescription.Deserialize(pk, s);

                // Save the self-reference in the internal storage to detect the changes
                sd.ServerDescriptionCid = sod.ServerDescriptionCid;
                sd.DBUpdate();
            }

            string SenderPeerID = SenderPeer.Id.ToString();

            // Maybe it's the first time where we've met. Need to check the background.
            // Or, the server changed its face.
            ServerDescription savedDescription = ServerDescription.DBLookup(SenderPeerID);
            if (savedDescription == null)
                await UpdateSD(sod, SenderPeerID);
            else if (savedDescription.ServerDescriptionCid != sod.ServerDescriptionCid)
                await UpdateSD(sod, SenderPeerID);

            // Set on receive, no sense to transmit the actual time.
            // In this context, latencies don't matter.
            sod.LastOnline = DateTime.Now;

            // Put it in the memory mapping.
            sod.DBInsert(SenderPeerID);

            return true;
        }

        private async Task OnDiscoveredPeer(Peer found)
        {
            if (DiscoveredPeers.ContainsKey(found.Id)) return;
            DiscoveredPeers[found.Id] = found;

            if (found.Id == self.Id)
            {
                Debug.Log($"Discovered node {found.Id} is self, skipping.");
                return;
            }

            if (!found.Addresses.Any())
            {
                Debug.Log($"Discovered node {found.Id} has no addresses, skipping.");
                return;
            }

            ServerDescription serverDescription = ServerDescription.DBLookup(found.Id.ToString());
            if(serverDescription == null)
            {
                Debug.Log($"Discovered node {found.Id} is new - connecting and expecting its server description");
                using CancellationTokenSource cts = new(1000);
                await ipfs.Swarm.ConnectAsync(found.Addresses.First(), cts.Token);
            }
            else
                Debug.Log($"Discovered node {found.Id} added.");
        }
        #endregion
        // ---------------------------------------------------------------
        #region IPFS Lowlevel interface
        public override Task PinCid_(Cid cid, bool pinned, CancellationToken token = default)
        {
            if (pinned)
                return ipfs.Pin.AddAsync(cid, cancel: token);
            else
                return ipfs.Pin.RemoveAsync(cid, cancel: token);
        }
        #endregion
    }
}
