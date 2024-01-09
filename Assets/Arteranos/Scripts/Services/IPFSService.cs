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

namespace Arteranos.Services
{
    public class IPFSService : MonoBehaviour
    {
        public IpfsEngine Ipfs { get => ipfs; }
        public Peer Self { get => self; }
        public SignKey ServerKeyPair { get => serverKeyPair; }
        public static string CachedPTOSNotice { get; private set; } = null;

        public event Action<IPublishedMessage> OnReceivedHello;
        public event Action<IPublishedMessage> OnReceivedServerDirectMessage;

        private const string PATH_USER_PRIVACY_NOTICE = "Privacy_TOS_Notice.md";

        private const string passphrase = "this is not a secure pass phrase";

        private const string topic_hello = "/X-Arteranos/Server-Hello";
        private const string topic_sdm = "/X-Arteranos/ToYou";

        private IpfsEngine ipfs = null;
        private Peer self = null;
        private SignKey serverKeyPair = null;

        private string versionString = null;
        private string minVersionString = null;

        _ServerDescription sd = null;
        private Cid currentSDCid = null;

        private CancellationTokenSource cts = null;

        private async void Start()
        {
            cts = new();

            // If it doesn't exist, write down the template in the config directory.
            if (!FileUtils.ReadConfig(PATH_USER_PRIVACY_NOTICE, File.Exists))
            {
                FileUtils.WriteTextConfig(PATH_USER_PRIVACY_NOTICE, Core.Utils.LoadDefaultTOS());
                Debug.LogWarning("Privacy notice and Terms Of Service template written down - Read (and modify) according to your use case!");
            }

            CachedPTOSNotice = FileUtils.ReadTextConfig(PATH_USER_PRIVACY_NOTICE);

            versionString = Core.Version.Load().MMP;
            minVersionString = Core.Version.VERSION_MIN;

            int port = SettingsManager.Server.MetadataPort;
            port = 12345; // DEBUG

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
                    if(success) OnReceivedHello?.Invoke(msg);
                }, 
                cts.Token);

            await ipfsTmp.PubSub.SubscribeAsync($"{topic_sdm}/{self.Id}",
                async msg =>
                {
                    if (msg.Sender.Id == self.Id) return;
                    bool success = await ParseIncomingIPFSMessageAsync(msg);
                    if (success) OnReceivedServerDirectMessage?.Invoke(msg);
                }, 
                cts.Token);

            KeyChain kc = await ipfsTmp.KeyChainAsync();
            var kcp = await kc.GetPrivateKeyAsync("self");
            serverKeyPair = SignKey.ImportPrivateKey(kcp);

            ipfs = ipfsTmp;

            await FlipServerDescription(true);
        }

        private async void OnDestroy()
        {
            await FlipServerDescription(false);

            await ipfs.StopAsync().ConfigureAwait(false);

            cts?.Cancel();

            cts?.Dispose();
        }

        public async Task FlipServerDescription(bool reload)
        {
            if(currentSDCid != null)
                await Ipfs.Block.RemoveAsync(currentSDCid);

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

            using (MemoryStream ms = new())
            {
                sd.Serialize(serverKeyPair, ms);
                ms.Position = 0;
                var fsn = await ipfs.FileSystem.AddAsync(ms, "ServerDescription");
                currentSDCid = fsn.Id;
            }
        }
        public async Task SendServerHello()
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
            await ipfs.PubSub.PublishAsync(topic_hello, ms);
        }

        public async Task SendServerDirectMessage(string peerId)
        {
            using CancellationTokenSource cts = new(100);
            await ipfs.PubSub.PublishAsync($"{topic_sdm}/{peerId}", "hello", cts.Token);
        }

        public async Task WaitForIPFSAsync()
        {
            while(Ipfs == null)
                await Task.Delay(100);
        }

        public IEnumerator WaitForIPFSCoRo()
        {
            while (ipfs == null)
                yield return new WaitForEndOfFrame();
        }

        public async Task<bool> ParseIncomingIPFSMessageAsync(IPublishedMessage publishedMessage)
        {
            try
            {
                PeerMessage peerMessage = PeerMessage.Deserialize(publishedMessage.DataStream);

                if (peerMessage is ServerHello sh)
                    return await ParseServerHelloAsync(sh);
                else
                    throw new ArgumentException($"Unknown message from Peer {publishedMessage.Sender.Id}");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
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
                    _ServerDescription old = _ServerDescription.DBLookup(link.PeerID);

                    if (old != null && old.PeerID != link.PeerID)
                        throw new ArgumentException($"{old.PeerID} mismatches {link.PeerID}");

                    // Skip data retrieval if we see if it's already outdated data
                    if (old == null || link.LastModified > old.LastModified)
                    {
                        using CancellationTokenSource cts = new(500);

                        Stream s = await ipfs.FileSystem.ReadFileAsync(link.ServerDescriptionCid, cts.Token);

                        PublicKey pk = PublicKey.FromId(link.PeerID);
                        _ServerDescription sd = _ServerDescription.Deserialize(pk, s);

                        if (sd.DBUpdate()) enteredCount--;
                    }
                    // else Debug.LogWarning($"Skipping outdated {link.PeerID}");
                }
                catch(Exception ex) { Debug.LogException(ex); }

            }

            return true;
        }
    }
}
