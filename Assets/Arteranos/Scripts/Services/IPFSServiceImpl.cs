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

namespace Arteranos.Services
{
    public class IPFSServiceImpl : IPFSService
    {
        public override IpfsEngine Ipfs_ { get => ipfs; }
        public override Peer Self_ { get => self; }
        public override SignKey ServerKeyPair_ { get => serverKeyPair; }
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
        private string minVersionString = null;

        ServerDescription sd = null;
        private Cid currentSDCid = null;

        private CancellationTokenSource cts = null;

        private List<byte[]> UserFingerprints = new();

        private async void Start()
        {
            Instance = this;

            last = DateTime.MinValue;

            cts = new();

            // If it doesn't exist, write down the template in the config directory.
            if (!FileUtils.ReadConfig(PATH_USER_PRIVACY_NOTICE, File.Exists))
            {
                FileUtils.WriteTextConfig(PATH_USER_PRIVACY_NOTICE, SettingsManager.DefaultTOStext);
                Debug.LogWarning("Privacy notice and Terms Of Service template written down - Read (and modify) according to your use case!");
            }

            CachedPTOSNotice = FileUtils.ReadTextConfig(PATH_USER_PRIVACY_NOTICE);

            versionString = Core.Version.Load().MMP;
            minVersionString = Core.Version.VERSION_MIN;

            int port = SettingsManager.Server.MetadataPort;
            port = 0; // DEBUG - Possible to leave it to a random port?

            IpfsEngine ipfsTmp;
            ipfsTmp = new(passphrase.ToCharArray());

            ipfsTmp.Options.Repository.Folder = Path.Combine(FileUtils.persistentDataPath, "IPFS");
            ipfsTmp.Options.KeyChain.DefaultKeyType = "ed25519";
            ipfsTmp.Options.KeyChain.DefaultKeySize = 2048;
            await ipfsTmp.Config.SetAsync(
                "Addresses.Swarm",
                JToken.FromObject(new string[] { $"/ip4/0.0.0.0/tcp/{port}" })
            );

            await ipfsTmp.StartAsync().ConfigureAwait(false);

            self = await ipfsTmp.LocalPeer;

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

            await FlipServerDescription_(true);

            UserFingerprints = new List<byte[]>();

            _ = EmitServerHeartbeat(cts.Token);

            SettingsManager.StartCoroutineAsync(GetUserListCoroutine);
        }

        private async void OnDestroy()
        {
            await FlipServerDescription_(false);

            await ipfs.StopAsync().ConfigureAwait(false);

            cts?.Cancel();

            cts?.Dispose();

            Instance = null;
        }

        private IEnumerator GetUserListCoroutine()
        {
            while(true)
            {
                UserFingerprints = (from user in NetworkStatus.GetOnlineUsers()
                                             where user.UserPrivacy != null && user.UserPrivacy.Visibility != Visibility.Invisible
                                             select CryptoHelpers.GetFingerprint(user.UserID)).ToList();

                yield return new WaitForSeconds(heartbeatSeconds / 2);
            }
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

        public override async Task<IPAddress> GetPeerIPAddress_(string PeerID, CancellationToken token = default)
        {
            Peer found = await ipfs.Dht.FindPeerAsync(PeerID, token).ConfigureAwait(false);

            if (found.ConnectedAddress == null)
                await ipfs.Swarm.ConnectAsync(found.Addresses.First(), token);

            string[] parts = found.ConnectedAddress.ToString().Split('/');
            if(parts.Length < 3)
                throw new Exception($"Cannot work with {found.Id}'s address of {found.ConnectedAddress}");

            return parts[1] switch
            {
                "ip6" or "ip4" => IPAddress.Parse(parts[2]),
                _ => throw new Exception($"Peer address {found.ConnectedAddress} is unworkable"),
            };
        }

        public override async Task FlipServerDescription_(bool reload)
        {
            if(currentSDCid != null)
                await Ipfs_.Block.RemoveAsync(currentSDCid);

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
                Icon = server.Icon,
                Version = versionString,
                MinVersion = minVersionString,
                Permissions = server.Permissions,
                PrivacyTOSNotice = CachedPTOSNotice,
                AdminNames = q.ToArray(),
                PeerID = self.Id.ToString(),
                LastModified = server.ConfigTimestamp
            };

            using MemoryStream ms = new();
            sd.Serialize(serverKeyPair, ms);
            ms.Position = 0;
            var fsn = await ipfs.FileSystem.AddAsync(ms, "ServerDescription");
            currentSDCid = fsn.Id;
        }

        public override Task SendServerOnlineData_()
        {
            // Flood mitigation
            if(last > DateTime.Now - TimeSpan.FromSeconds(30)) return Task.CompletedTask;
            last = DateTime.Now;

            ServerOnlineData sod = new()
            {
                WorldInfoCid = SettingsManager.WorldInfoCid,
                ServerDescriptionCid = currentSDCid,
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
            ServerHello.SDLink selflink = new()
            {
                ServerDescriptionCid = currentSDCid,
                LastModified = SettingsManager.Server.ConfigTimestamp,
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
        }

        public override Task SendServerDirectMessage_(string peerId, PeerMessage message)
        {
            using CancellationTokenSource cts = new(100);
            using MemoryStream ms = new();

            message.Serialize(ms);
            ms.Position = 0;
            return ipfs.PubSub.PublishAsync($"{topic_sdm}/{peerId}", ms, cts.Token);
        }

        public override Task PinCid_(Cid cid, bool pinned, CancellationToken token = default)
        {
            if(pinned)
                return ipfs.Pin.AddAsync(cid, cancel: token);
            else
                return ipfs.Pin.RemoveAsync(cid, cancel: token);
        }

        public override Task<IEnumerable<Cid>> ListPinned_(CancellationToken token = default)
        {
            return ipfs.Pin.ListAsync(token);
        }

        public Task<bool> ParseIncomingIPFSMessageAsync(IPublishedMessage publishedMessage)
        {
            try
            {
                PeerMessage peerMessage = PeerMessage.Deserialize(publishedMessage.DataStream);

                if (peerMessage is ServerHello sh)
                    return ParseServerHelloAsync(sh);
                else if (peerMessage is ServerOnlineData sod)
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

        private async Task<bool> ParseServerOnlineData(ServerOnlineData sod, Peer SenderPeer)
        {
            string SenderPeerID = SenderPeer.Id.ToString();

            // Maybe it's the first time where we've met. Need to check the background.
            if(ServerDescription.DBLookup(SenderPeerID) == null)
            {
                using CancellationTokenSource cts = new(500);

                Stream s = await ipfs.FileSystem.ReadFileAsync(sod.ServerDescriptionCid, cts.Token);

                PublicKey pk = PublicKey.FromId(SenderPeerID);
                ServerDescription sd = ServerDescription.Deserialize(pk, s);

                sd.DBUpdate();
            }

            // Set on receive, no sense to transmit the actual time.
            // In this context, latencies don't matter.
            sod.LastOnline = DateTime.Now;

            // Put it in the memory mapping.
            sod.DBInsert(SenderPeerID);

            return true;
        }
    }
}
