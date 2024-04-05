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
using System.Net;
using UnityEditor;

namespace Arteranos.PlayTest.Services
{
#if false
    public class IPFS
    {
        IPFSServiceImpl srv = null;
        IpfsEngine ipfs = null;
        Peer self = null;

        public async Task<(ServerDescription, ServerHello)> CreateServerHello(IpfsEngine node)
        {
            Peer nodeself = await node.LocalPeer;

            DateTime unixEpoch = DateTime.UnixEpoch;
            ServerDescription sd = new()
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
                PeerID = nodeself.Id.ToString(),
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
                PeerID = nodeself.Id.ToString(),
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
            srv = go2.AddComponent<IPFSServiceImpl>();

            yield return null;

            yield return TestFixture.WaitForCondition(5, () => srv?.Ipfs_ != null, "IPFS server timeout");

            ipfs = srv.Ipfs_;

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

            var go2 = GameObject.FindObjectOfType<IPFSServiceImpl>();
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
                srv.OnReceivedHello_ += Receiver;
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
                srv.OnReceivedHello_ -= Receiver;

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

                await srv.SendServerHello_();

                await TestFixture.WaitForConditionAsync(5, () => (hello != null), "Message was not received");

                Assert.IsNotNull(hello.Links);
                Assert.IsTrue(hello.Links.Any());

                ServerHello.SDLink link = hello.Links.First();

                using CancellationTokenSource cts = new(1000);

                Stream s = await otherNode.FileSystem.ReadFileAsync(link.ServerDescriptionCid, cts.Token);

                PublicKey pk = PublicKey.FromId(sender.Id);
                ServerDescription sd = ServerDescription.Deserialize(pk, s);

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

                (ServerDescription sd, ServerHello hello) = await CreateServerHello(otherNode);

                using MemoryStream ms = new();

                hello.Serialize(ms);
                ms.Position = 0;
                await otherNode.PubSub.PublishAsync("/X-Arteranos/Server-Hello", ms.ToArray(), cts.Token);

                await Task.Delay(5000);

            }
            finally { await otherNode.StopAsync(); }
        }

        [UnityTest]
        public IEnumerator GetPeerIPAddress()
        {
            Task.Run(GetPeerIPAddressAsync).Wait();

            yield return null;
        }

        public async Task GetPeerIPAddressAsync()
        {
            // Found in the log of Unity-IPFS-Engine, ReadFileFromNetwork test, searched for 'Alive'
            // QmS4ustL54uo8FzR9455qaxZwuMiUhyvMcX9Ba8nUH4uVv ('About IPFS' file)
            string peerID = "12D3KooWJXJedrSscwoMZEWcRqCtZJgvdEvWPT7BT9x8UEugRDqQ";
            // string peerID = "QmcZf59bWwK5XFi76CZX8cbJ4BhTzzA3gU1ZjYZcYW3dwt";

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(20));

            IPAddress addr = await srv.GetPeerIPAddress_(peerID, cts.Token);

            Debug.Log($"Peer's address is {addr}");
        }

        [UnityTest]
        public IEnumerator GetPeerIPAddress_Negative()
        {
            Task.Run(GetPeerIPAddress_NegativeAsync).Wait();

            yield return null;
        }

        public async Task GetPeerIPAddress_NegativeAsync()
        {
            string wrongPeerID = "12D3KooWJXJedrSscwoMZEWcRqCtZJgvownedT7BT9x8UEugRDqQ";
            // string peerID = "QmcZf59bWwK5XFi76CZX8cbJ4BhTzzA3gU1ZjYZcYW3dwt";

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(20));

            try
            {
                IPAddress addr = await srv.GetPeerIPAddress_(wrongPeerID, cts.Token);

                Debug.Log($"Peer's address is {addr}");

                Assert.Fail("Didn't throw");
            }
            catch (Exception ex)
            {
                Debug.Log($"Expected exception: {ex.Message}");
            }
        }
    }
#endif

    public class IPFS
    {
        [UnitySetUp]
        public IEnumerator SetupIPFS()
        {
            //Camera ca;
            //Light li;

            //ca = new GameObject("Camera").AddComponent<Camera>();
            //ca.transform.position = new(0, 1.75f, 0.2f);

            //li = new GameObject("Light").AddComponent<Light>();
            //li.transform.SetPositionAndRotation(new(0, 3, 0), Quaternion.Euler(50, -30, 0));
            //li.type = LightType.Directional;
            //li.color = Color.white;

            GameObject bp = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arteranos/Prefabs/Core/_SceneEssentials.prefab");
            GameObject go = UnityEngine.Object.Instantiate(bp);

            // Resynchronize with the background IPFS uploading processes
            yield return new WaitUntil(() => SettingsManager.DefaultFemaleAvatar != null);

            // yield return UploadTestWorld();
        }

        [UnityTearDown]
        public IEnumerator CleanupIPFS()
        {
            GameObject go = SettingsManager.Instance.gameObject;
            UnityEngine.Object.Destroy(go);

            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
        }

        [UnityTest]
        public IEnumerator TestFixtureTest()
        {
            yield return null;

            Assert.True(true);
        }

        [UnityTest]
        public IEnumerator ConnectivityTest()
        {
            try
            {
                yield return null;

                MultiHash RemotePeerId = IPFSService.Self.Id;

                MultiAddress selfExternalAddress = $"/ip4/{NetworkStatus.ExternalAddress}/tcp/{SettingsManager.Server.MetadataPort}/p2p/{RemotePeerId}";

                Debug.Log($"IPFS server multiaddress: {selfExternalAddress}");

                NetworkStatus.OpenPorts = true;

                yield return new WaitForSeconds(5);

                using TempNode outsider = new();
                Task t0 = outsider.StartAsync();
                yield return new WaitUntil(() => t0.IsCompleted);

                Peer other = outsider.LocalPeer.Task.Result;

                using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));
                Task t1 = outsider.Swarm.ConnectAsync(selfExternalAddress, cts.Token);
                yield return new WaitUntil(() => t1.IsCompleted);

                Peer[] peers = IPFSService.Instance.Ipfs_.Swarm.PeersAsync().Result.ToArray();

                Assert.IsNotNull(peers.Length);

                int i = 0, j = 0;
                int found_i = -1;
                foreach (Peer peer in peers)
                {
                    i++;
                    j = 0;
                    foreach (MultiAddress address in peer.Addresses)
                    {
                        j++;
                        Debug.Log($"IPFS server's peer {i}, Address {j}: {address}");
                    }
                    if (peer.Id == other.Id) found_i = i;
                }
                Assert.IsTrue(found_i >= 0);
                Debug.Log($"Outsider's contact was found in {found_i}");

                i = 0;
                found_i = -1;
                Peer[] peers2 = outsider.Swarm.PeersAsync().Result.ToArray();
                foreach (Peer peer in peers2)
                {
                    i++;
                    j = 0;
                    foreach(MultiAddress address in peer.Addresses)
                    {
                        j++;
                        Debug.Log($"Outsider's peer {i}, Address {j}: {address}");
                    }
                }
            }
            finally 
            {
                NetworkStatus.OpenPorts = false;
            }
        }

        [UnityTest]
        public IEnumerator LoopbackConnectivityTest()
        {
            yield return null;

            MultiHash RemotePeerId = IPFSService.Self.Id;

            MultiAddress selfExternalAddress = $"/ip4/127.0.0.1/tcp/{SettingsManager.Server.MetadataPort}/p2p/{RemotePeerId}";

            Debug.Log($"IPFS server multiaddress: {selfExternalAddress}");

            using TempNode outsider = new();
            Task t0 = outsider.StartAsync();
            yield return new WaitUntil(() => t0.IsCompleted);

            Peer other = outsider.LocalPeer.Task.Result;

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));
            Task t1 = outsider.Swarm.ConnectAsync(selfExternalAddress, cts.Token);
            yield return new WaitUntil(() => t1.IsCompleted);

            Peer[] peers = IPFSService.Instance.Ipfs_.Swarm.PeersAsync().Result.ToArray();

            Assert.IsNotNull(peers.Length);

            bool found = false;
            int i = 0, j = 0;
            foreach (Peer peer in peers)
            {
                i++;
                j = 0;
                if (peer.Id == other.Id) found = true;
                foreach (MultiAddress address in peer.Addresses)
                {
                    j++;
                    Debug.Log($"IPFS server's peer {i}, Address {j}: {address}");
                }
            }
            Assert.IsTrue(found);
        }

        [UnityTest]
        public IEnumerator FindExistingServerTest()
        {
            yield return null;

            IpfsEngine myserver = IPFSService.Instance.Ipfs_;

            // Put up the file
            Task<IFileSystemNode> taskFsn = myserver.FileSystem.AddTextAsync(Core.Version.VERSION_MIN);
            yield return new WaitUntil(() => taskFsn.IsCompleted);
            IFileSystemNode fileSystemNode = taskFsn.Result;
            Cid cid = fileSystemNode.Id;

            yield return new WaitForSeconds(10);

            // Set up a node, with its complete bootstrap service
            using TempNode outsider = new(useBS: true);
            Task t0 = outsider.StartAsync();
            yield return new WaitUntil(() => t0.IsCompleted);
            Peer other = outsider.LocalPeer.Task.Result;

            // Announce its presence
            Task taskProvide = myserver.Dht.ProvideAsync(cid, true);
            yield return new WaitUntil(() => taskProvide.IsCompleted);

            Debug.Log($"Identify file CID: {cid}");


            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(120));

            // Try to find the requested file, independent with its prior knowledge
            // save for its Cid.
            Task<string> taskContent = outsider.FileSystem.ReadAllTextAsync(cid, cts.Token);
            yield return new WaitUntil(() => taskContent.IsCompleted);
            string content = taskContent.Result;
            Assert.AreEqual(content, Core.Version.VERSION_MIN);
        }

        [UnityTest]
        public IEnumerator FindServersTest()
        {
            yield return null;

            IpfsEngine myserver = IPFSService.Instance.Ipfs_;
            Core.Version version = Core.Version.Load();

            // Put up the file
            Cid cid = null;
            yield return Utils.Async2Coroutine(myserver.FileSystem.AddTextAsync(version.Full), _fsn => cid = _fsn.Id);

            yield return new WaitForSeconds(10);

            // Set up a node, with its complete bootstrap service
            using TempNode outsider = new(useBS: true);
            Peer other = null;

            yield return Utils.Async2Coroutine(outsider.StartAsync());
            yield return Utils.Async2Coroutine(outsider.LocalPeer.Task, _peer => other = _peer);

            // Announce its presence
            yield return Utils.Async2Coroutine(myserver.Dht.ProvideAsync(cid, true));

            Debug.Log($"Identify file CID: {cid}");

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(60));

            int found = 0;
            Task<IEnumerable<Peer>> taskPeers = outsider.Dht.FindProvidersAsync(cid, 1, _peer =>
            {
                found++;
                Debug.Log($"Found: {_peer.Id}");
                foreach (MultiAddress address in _peer.Addresses)
                    Debug.Log($"  Address: {address}");
            }, cts.Token);

            // Wait until we have it run through -- two minutes or 20 servers, whichever it's
            // earlier.
            yield return new WaitUntil(() => taskPeers.IsCompleted);

            Assert.IsTrue(found > 0);
        }

    }
}