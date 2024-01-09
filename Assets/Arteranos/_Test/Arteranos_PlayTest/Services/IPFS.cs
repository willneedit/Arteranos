using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using Ipfs.Engine;
using Arteranos.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using Arteranos.Core;
using Ipfs;
using System.Linq;
using System.Threading;
using System.Text;
using Ipfs.Core.Cryptography.Proto;
using Ipfs.Engine.Cryptography;
using Arteranos.Core.Cryptography;

namespace Arteranos.PlayTest.Services
{
    public class IPFS
    {
        IPFSService srv = null;
        IpfsEngine ipfs = null;
        Peer self = null;

        public async Task<(_ServerDescription, ServerHello)> CreateServerHello(IpfsEngine node)
        {
            DateTime unixEpoch = DateTime.UnixEpoch;
            _ServerDescription sd = new()
            {
                Name = "Snake Oil",
                ServerPort = 1,
                MetadataPort = 2,
                Description = "Snake Oil Inc.",
                Icon = new byte[0],
                Version = "0.0.1",
                MinVersion = "0.0.1",
                Permissions = new(),
                PrivacyTOSNotice = "TODO",      // TODO
                AdminNames = new string[0],     // TODO
                PeerID = self.Id.ToString(),
                LastModified = unixEpoch
            };
            KeyChain kc = await node.KeyChainAsync();
            var kcp = await kc.GetPrivateKeyAsync("self");
            SignKey serverKeyPair = SignKey.ImportPrivateKey(kcp);

            Cid currentSDCid;
            using (MemoryStream ms = new())
            {
                sd.Serialize(serverKeyPair, ms);
                ms.Position = 0;
                var fsn = await node.FileSystem.AddAsync(ms, "ServerDescription");
                currentSDCid = fsn.Id;
            }

            ServerHello.SDLink selflink = new()
            {
                ServerDescriptionCid = currentSDCid,
                LastModified = unixEpoch,
                PeerID = self.Id.ToString(),
            };

            ServerHello hello = new()
            {
                Links = new() { selflink }
            };

            return (sd, hello);
        }

        [UnitySetUp]
        public IEnumerator SetupIPFS()
        {
            GameObject go1 = new GameObject("SettingsManager");
            StartupManagerMock sm = go1.AddComponent<StartupManagerMock>();

            yield return null;

            GameObject go2 = new GameObject("IPFS Service");
            srv = go2.AddComponent<IPFSService>();

            yield return null;

            yield return TestFixture.WaitForCondition(5, () => srv?.Ipfs != null, "IPFS server timeout");

            ipfs = srv.Ipfs;

            self = Task.Run(async () => await ipfs.LocalPeer).Result;
        }

        [UnityTearDown]
        public IEnumerator TeardownIPFS()
        {
            srv = null;
            ipfs = null;
            self = null;

            var go1 = GameObject.FindObjectOfType<StartupManagerMock>();
            GameObject.Destroy(go1.gameObject);

            var go2 = GameObject.FindObjectOfType<IPFSService>();
            GameObject.Destroy(go2.gameObject);

            yield return null;
        }

        [UnityTest]
        public IEnumerator Startup()
        {
            yield return new WaitForSeconds(1);
        }

        [UnityTest]
        public IEnumerator SensingNewServer()
        {
            Task.Run(SensingServerHelloAsync).Wait();

            yield return null;
        }

        private async Task SensingServerHelloAsync()
        {
            Peer sender = null;

            void Receiver(IPublishedMessage message)
            {
                sender = message.Sender;
            }

            using TempNode otherNode = new TempNode();

            try
            {
                srv.OnReceivedHello += Receiver;
                await otherNode.StartAsync();
                Peer other = await otherNode.LocalPeer;

                await otherNode.Swarm.ConnectAsync(self.Addresses.First());

                using CancellationTokenSource cts = new(100);

                await otherNode.PubSub.PublishAsync("/X-Arteranos/Server-Hello", "hi", cts.Token);

                await TestFixture.WaitForConditionAsync(5, () => (sender != null), "Message was not received");

                Assert.AreEqual(sender.Id, other.Id);

            }
            finally
            {
                srv.OnReceivedHello -= Receiver;

                await otherNode.StopAsync();
            }
        }

        [UnityTest]
        public IEnumerator ClientGetsServerInfo() 
        { 
            Task.Run(ClientGetsServerInfoAsync).Wait();

            yield return null;
        }

        public async Task ClientGetsServerInfoAsync()
        {
            Peer sender = null;
            ServerHello hello = null;

            void Receiver(IPublishedMessage message)
            {
                sender = message.Sender;

                PeerMessage msg = PeerMessage.Deserialize(message.DataStream);

                Assert.IsInstanceOf<ServerHello>(msg);

                hello = msg as ServerHello;
            }

            using TempNode otherNode = new TempNode();

            try
            {
                await otherNode.StartAsync();

                using CancellationTokenSource subscriber = new();

                await otherNode.PubSub.SubscribeAsync("/X-Arteranos/Server-Hello", Receiver, subscriber.Token);

                await otherNode.Swarm.ConnectAsync(self.Addresses.First());

                await srv.SendServerHello();

                await TestFixture.WaitForConditionAsync(5, () => (hello != null), "Message was not received");

                Assert.IsNotNull(hello.Links);
                Assert.IsTrue(hello.Links.Any());

                ServerHello.SDLink link = hello.Links.First();

                using CancellationTokenSource cts = new(1000);

                Stream s = await otherNode.FileSystem.ReadFileAsync(link.ServerDescriptionCid, cts.Token);

                PublicKey pk = PublicKey.FromId(sender.Id);
                _ServerDescription sd = _ServerDescription.Deserialize(pk, s);

                Assert.IsNotNull(sd);
                Assert.AreEqual(sd.LastModified, link.LastModified);
                Debug.Log(sd.Name);
                Debug.Log(link.LastModified);
            }
            finally
            {
                await otherNode.StopAsync();
            }
        }

        [UnityTest]
        public IEnumerator ReceiveServerHello()
        {
            Task.Run(ReceiveServerHelloAsync).Wait();

            yield return null;
        }

        public async Task ReceiveServerHelloAsync()
        {
            using TempNode otherNode = new TempNode();

            try
            {
                await otherNode.StartAsync();

                Peer other = await otherNode.LocalPeer;

                await otherNode.Swarm.ConnectAsync(self.Addresses.First());

                using CancellationTokenSource cts = new(2000);

                (_ServerDescription sd, ServerHello hello) = await CreateServerHello(otherNode);

                using MemoryStream ms = new();

                hello.Serialize(ms);
                ms.Position = 0;
                await otherNode.PubSub.PublishAsync("/X-Arteranos/Server-Hello", ms.ToArray(), cts.Token);

                await Task.Delay(5000);

            }
            finally { await otherNode.StopAsync(); }
        }
    }
}