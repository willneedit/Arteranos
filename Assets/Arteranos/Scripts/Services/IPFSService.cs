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

namespace Arteranos.Services
{
    public class IPFSService : MonoBehaviour
    {
        private const string passphrase = "this is not a secure pass phrase";

        private const string topic_hello = "/X-Arteranos/Server-Hello";
        private const string topic_sdm = "/X-Arteranos/ToYou";

        public IpfsEngine Ipfs { get => ipfs; }
        public Peer Self { get => self; }
        public SignKey ServerKeyPair { get => serverKeyPair; }


        public event Action<IPublishedMessage> OnReceivedHello;
        public event Action<IPublishedMessage> OnReceivedServerDirectMessage;

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
                msg => OnReceivedHello?.Invoke(msg), 
                cts.Token);

            await ipfsTmp.PubSub.SubscribeAsync($"{topic_sdm}/{self.Id}",
                msg => OnReceivedServerDirectMessage?.Invoke(msg), 
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
                PrivacyTOSNotice = "TODO",      // TODO
                AdminNames = new string[0],     // TODO
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
            ServerHello hello = new()
            {
                ServerDescriptionCid = currentSDCid,
                LastModified = SettingsManager.Server.ConfigTimestamp
            };

            hello.Serialize(out byte[] bytes);
            await ipfs.PubSub.PublishAsync(topic_hello, bytes);
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
    }
}
