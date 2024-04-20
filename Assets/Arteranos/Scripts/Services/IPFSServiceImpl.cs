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
using Ipfs.Engine;
using System.IO;
using Newtonsoft.Json.Linq;
using Arteranos.Core;
using System.Threading;
using System;
using System.Threading.Tasks;
using Ipfs.Engine.Cryptography;
using Arteranos.Core.Cryptography;
using System.Linq;
using Ipfs.Core.Cryptography.Proto;
using System.Net;
using System.Text;
using System.Collections.Concurrent;

namespace Arteranos.Services
{
    public class IPFSServiceImpl : IPFSService
    {
        public override IpfsEngine Ipfs_ { get => ipfs; }
        public override Peer Self_ { get => self; }
        public override SignKey ServerKeyPair_ { get => serverKeyPair; }
        public override Cid IdentifyCid_ { get; protected set; }
        public override Cid CurrentSDCid_ { get; protected set; } = null;

        public static string CachedPTOSNotice { get; private set; } = null;

        public override event Action<IPublishedMessage> OnReceivedHello_;
        public override event Action<IPublishedMessage> OnReceivedServerDirectMessage_;

        private const string PATH_USER_PRIVACY_NOTICE = "Privacy_TOS_Notice.md";

        private const string passphrase = "this is not a secure pass phrase";

        private const string topic_hello = "/X-Arteranos/Server-Hello";
        private const string topic_sdm = "/X-Arteranos/ToYou";

        private const int heartbeatSeconds = 60;

        private IpfsEngine ipfs = null;
        private Peer self = null;
        private SignKey serverKeyPair = null;

        private DateTime last = DateTime.MinValue;

        private string versionString = null;

        ServerDescription sd = null;

        private CancellationTokenSource cts = null;

        private List<byte[]> UserFingerprints = new();

        private ConcurrentDictionary<MultiHash, Peer> DiscoveredPeers = null;

        // ---------------------------------------------------------------
        #region Start & Stop
        private async void Start()
        {
            Instance = this;
            IdentifyCid_ = null;
            last = DateTime.MinValue;
            DiscoveredPeers = new();
            cts = new();

            // If it doesn't exist, write down the template in the config directory.
            if (!FileUtils.ReadConfig(PATH_USER_PRIVACY_NOTICE, File.Exists))
            {
                FileUtils.WriteTextConfig(PATH_USER_PRIVACY_NOTICE, SettingsManager.DefaultTOStext);
                Debug.LogWarning("Privacy notice and Terms Of Service template written down - Read (and modify) according to your use case!");
            }

            CachedPTOSNotice = FileUtils.ReadTextConfig(PATH_USER_PRIVACY_NOTICE);

            versionString = Core.Version.Load().MMP;

            int port = SettingsManager.Server.MetadataPort;

            IpfsEngine ipfsTmp;
            ipfsTmp = new(passphrase.ToCharArray());

            ipfsTmp.Options.Repository.Folder = Path.Combine(FileUtils.persistentDataPath, "IPFS");
            ipfsTmp.Options.KeyChain.DefaultKeyType = "ed25519";
            ipfsTmp.Options.KeyChain.DefaultKeySize = 2048;
            await ipfsTmp.Config.SetAsync(
                "Addresses.Swarm",
                JToken.FromObject(new string[] { 
                    $"/ip4/0.0.0.0/tcp/{port}",
                    $"/ip6/::/tcp/{port}"
                })
            );

            await ipfsTmp.StartAsync().ConfigureAwait(false);

            self = await ipfsTmp.LocalPeer;

            // Wait for the public IP address determination
            DateTime tmo = DateTime.UtcNow + TimeSpan.FromSeconds(5);
            while (NetworkStatus.PublicIPAddress == IPAddress.None && DateTime.UtcNow < tmo)
                await Task.Delay(500);

            // The IPFS node has to advertise itself with its public IP(v4) address to deal
            // with NAT - you wouldn't be able to connect.
            // 
            if (NetworkStatus.PublicIPAddress != IPAddress.None)
            {
                Debug.Log("Got the public IP address, setting up the IPFS node...");

                // Filter out the local IPv4 addresses
                List<MultiAddress> addresses = (from entry in self.Addresses
                                                   where !entry.ToString().StartsWith("/ip4/")
                                                   select entry).ToList();

                // And add an entry with the external IPv4 address.
                addresses.Add(                
                    GetMultiAddress(NetworkStatus.PublicIPAddress, port, self.Id)
                );

                self.Addresses = addresses;
            }

            await ipfsTmp.PubSub.SubscribeAsync(topic_hello,
                async msg =>
                {
                    if (msg.Sender.Id == self.Id) return;
                    bool success = await ParseIncomingIPFSMessageAsync(msg);
                    if(success) OnReceivedHello_?.Invoke(msg);
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

            await FlipServerDescription_(true);

            UserFingerprints = new List<byte[]>();

            _ = EmitServerHeartbeat(cts.Token);

            SettingsManager.StartCoroutineAsync(GetUserListCoroutine);

            SettingsManager.StartCoroutineAsync(DiscoverPeersCoroutine);
        }

        private async void OnDisable()
        {
            await FlipServerDescription_(false);

            Debug.Log("Shutting down the IPFS node.");

            await ipfs.StopAsync().ConfigureAwait(false);

            cts?.Cancel();

            cts?.Dispose();

            Instance = null;
        }

        private IEnumerator GetUserListCoroutine()
        {
            StringBuilder sb = new();
            sb.Append("Arteranos Server, built by willneedit\n");
            sb.Append(Core.Version.VERSION_MIN);

            yield return Utils.Async2Coroutine(ipfs.FileSystem.AddTextAsync(sb.ToString()), _fsn => IdentifyCid_ = _fsn.Id);

            while (true)
            {
                UserFingerprints = (from user in NetworkStatus.GetOnlineUsers()
                                             where user.UserPrivacy != null && user.UserPrivacy.Visibility != Visibility.Invisible
                                             select CryptoHelpers.GetFingerprint(user.UserID)).ToList();

                yield return new WaitForSeconds(heartbeatSeconds / 2);

                yield return Utils.Async2Coroutine(ipfs.Dht.ProvideAsync(IdentifyCid_, true));
            }
        }

        private IEnumerator DiscoverPeersCoroutine()
        {
            // Wait for the identifier file's CID to come up.
            yield return new WaitUntil(() => IdentifyCid != null);

            Debug.Log($"Starting node discovery: Identifier file's CID is {IdentifyCid}");

            while(true)
            {
                Task<IEnumerable<Peer>> taskPeers = ipfs.Dht.FindProvidersAsync(IdentifyCid, 
                    1000, // FIXME Maybe configurable.
                    _peer => _ = OnDiscoveredPeer(_peer));

                yield return Utils.Async2Coroutine(taskPeers);
            }
            // NOTREACHED
        }

        private async Task EmitServerHeartbeat(CancellationToken cancel)
        {
            while(!cancel.IsCancellationRequested)
            {
                if(NetworkStatus.GetOnlineLevel() == OnlineLevel.Server || 
                    NetworkStatus.GetOnlineLevel() == OnlineLevel.Host)
                {
                    try
                    {
                        await SendServerOnlineData_();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(heartbeatSeconds), cancel);
            }
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

        public override async Task FlipServerDescription_(bool reload)
        {
            if(CurrentSDCid_ != null)
                await Ipfs_.Block.RemoveAsync(CurrentSDCid_);

            if (!reload) return;

            Server server = SettingsManager.Server;
            IEnumerable<string> q = from entry in SettingsManager.ServerUsers.Base
                                    where UserState.IsSAdmin(entry.userState)
                                    select ((string)entry.userID);

            sd = new()
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
                ServerDescriptionCid = null // Self-reference will be generated AFTER putting it to IPFS
            };

            using MemoryStream ms = new();
            sd.Serialize(serverKeyPair, ms);
            ms.Position = 0;
            var fsn = await ipfs.FileSystem.AddAsync(ms, "ServerDescription");
            CurrentSDCid_ = fsn.Id;
        }

        public override Task SendServerOnlineData_()
        {
            // Flood mitigation
            if(last > DateTime.Now - TimeSpan.FromSeconds(30)) return Task.CompletedTask;
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
            return ipfs.PubSub.PublishAsync(topic_hello, ms);
        }

        public override Task SendServerHello_()
        {
#if USE_SERVER_HELLO
            ServerHello.SDLink selflink = new()
            {
                ServerDescriptionCid = CurrentSDCid_,
                LastModified = SettingsManager.Server.ConfigLastChanged,
                PeerID = self.Id.ToString(),
            };

            ServerHello hello = new()
            {
                Links = new() { selflink }
            };

            // TODO Add additional server descriptions to broadcast

            using MemoryStream ms = new();

            hello.Serialize(ms);
            ms.Position = 0;
            return ipfs.PubSub.PublishAsync(topic_hello, ms);
#else
            return Task.CompletedTask;
#endif
        }

        public override Task SendServerDirectMessage_(string peerId, PeerMessage message)
        {
            using CancellationTokenSource cts = new(100);
            using MemoryStream ms = new();

            message.Serialize(ms);
            ms.Position = 0;
            return ipfs.PubSub.PublishAsync($"{topic_sdm}/{peerId}", ms, cts.Token);
        }

        public Task<bool> ParseIncomingIPFSMessageAsync(IPublishedMessage publishedMessage)
        {
            try
            {
                PeerMessage peerMessage = PeerMessage.Deserialize(publishedMessage.DataStream);
#if USE_SERVER_HELLO
                if (peerMessage is ServerHello sh)
                    return ParseServerHelloAsync(sh);
                else 
#endif
                if (peerMessage is ServerOnlineData sod)
                    return ParseServerOnlineData(sod, publishedMessage.Sender);
                else
                    throw new ArgumentException($"Unknown message from Peer {publishedMessage.Sender.Id}");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return Task.FromResult(false);
            }
        }

#if USE_SERVER_HELLO
        private async Task<bool> ParseServerHelloAsync(ServerHello hello)
        {
            int enteredCount = 20;

            foreach (ServerHello.SDLink link in hello.Links)
            {
                // Too many...
                if (enteredCount <= 0) break;

                try
                {
                    ServerDescription old = ServerDescription.DBLookup(link.PeerID);

                    if (old != null && old.PeerID != link.PeerID)
                        throw new ArgumentException($"{old.PeerID} mismatches {link.PeerID}");

                    // Skip data retrieval if we see if it's already outdated data
                    if (old == null || link.LastModified > old.LastModified)
                    {
                        using CancellationTokenSource cts = new(500);

                        Stream s = await ipfs.FileSystem.ReadFileAsync(link.ServerDescriptionCid, cts.Token);

                        PublicKey pk = PublicKey.FromId(link.PeerID);
                        ServerDescription sd = ServerDescription.Deserialize(pk, s);

                        if (sd.DBUpdate()) enteredCount--;
                    }
                    // else Debug.LogWarning($"Skipping outdated {link.PeerID}");
                }
                catch(Exception ex) { Debug.LogException(ex); }

            }

            return true;
        }
#endif

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
            Debug.Log($"Discovered node {found.Id}");

            if (found.Id == self.Id)
            {
                Debug.Log("  Node is self, skipping.");
                return;
            }

            if (DiscoveredPeers.ContainsKey(found.Id))
            {
                Debug.Log("  Node is already known, skipping");
                return;
            }

            if (!found.Addresses.Any())
            {
                Debug.Log("  Node has no addresses, skipping.");
                return;
            }

            Debug.Log("  Adding node as discovered.");
            DiscoveredPeers[found.Id] = found;

            ServerDescription serverDescription = ServerDescription.DBLookup(found.Id.ToString());
            if(serverDescription == null)
            {
                Debug.Log("  New node - connecting and expecting its server description");
                using CancellationTokenSource cts = new(1000);
                await ipfs.Swarm.ConnectAsync(found.Addresses.First(), cts.Token);
            }
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
