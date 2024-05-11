using System.Collections;
using System.IO;
using System.Threading.Tasks;
using Arteranos.Core;
using Ipfs.Http;
using NUnit.Framework;
using UnityEngine.TestTools;
using Ipfs;
using Ipfs.Cryptography.Proto;
using Ipfs.Cryptography;
using System.Threading;
using System.Linq;

#if false

namespace Arteranos.PlayTest
{
    public class IPFSBackend
    {
        // A Test behaves as an ordinary method
        [Test]
        public void IPFSBackendSimplePasses()
        {
            // Use the Assert class to test conditions
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator IPFSBackendWithEnumeratorPasses()
        {
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return null;
        }

        [UnityTest]
        public IEnumerator ServiceExists()
        {
            yield return TestFixture.SetupIPFS();
            Assert.IsNotNull(TestFixture.Ipfs);

            Assert.IsNotNull(TestFixture.IPFSService.ServerKeyPair_);
            Assert.IsNotNull(TestFixture.IPFSService.Self_);
            Assert.AreEqual(TestFixture.IPFSService.Self_.Id, TestFixture.IPFSService.ServerKeyPair_.PublicKey.ToId());
        }

        [UnityTest]
        public IEnumerator SetupService()
        {
            yield return TestFixture.EnsureIPFSStopped();

            string IPFSRootFolder = Path.Combine(FileUtils.persistentDataPath, "IPFS");
            string IPFSBackupFolder = Path.Combine(FileUtils.persistentDataPath, "_IPFS");

            bool moved = false;
            try
            {
                if (Directory.Exists(IPFSRootFolder))
                {
                    moved = true;
                    Directory.Move(IPFSRootFolder, IPFSBackupFolder);
                }

                yield return TestFixture.SetupIPFS();
                Assert.IsNotNull(TestFixture.Ipfs);
                IpfsEngine ipfs = TestFixture.Ipfs;

                yield return TestFixture.WaitForCondition(
                    10,
                    () => Directory.Exists(IPFSRootFolder),
                    "Timeout while setting the IPFS directory");
            }
            finally
            {
                if (Directory.Exists(IPFSRootFolder))
                    Directory.Delete(IPFSRootFolder, true);

                if (moved)
                    Directory.Move(IPFSBackupFolder, IPFSRootFolder);
            }
        }

        [UnityTest]
        public IEnumerator ServerKeys()
        {
            yield return TestFixture.SetupIPFS();
            Assert.IsNotNull(TestFixture.Ipfs);

            Task.Run(ServerKeyAsync).Wait();
        }

        private async Task ServerKeyAsync()
        {
            IpfsEngine ipfs = TestFixture.Ipfs;

            Peer self = await ipfs.LocalPeer;

            PublicKey key = PublicKey.FromId(self.Id);
            Assert.IsNotNull(key);
            Assert.AreEqual(self.PublicKey, key);

            // "self" is this IPFS instance's local node's (a.k.a ipfs.LocalPeer's) key
            var kc = await ipfs.KeyChainAsync();
            var kcp = await kc.GetPrivateKeyAsync("self");
            KeyPair kp = KeyPair.Import(kcp);

            Assert.IsNotNull(kp);
            Assert.AreEqual(kp.PublicKey, key);
        }

        [UnityTest]
        public IEnumerator ReceiveServerHello()
        {
            yield return TestFixture.SetupIPFS();
            Assert.IsNotNull(TestFixture.Ipfs);

            Task.Run(ReceiveServerHelloAsync).Wait();
        }

        public async Task ReceiveServerHelloAsync()
        {
            Peer sender = null;

            void Receiver(IPublishedMessage message)
            {
                sender = message.Sender;
            }

            try
            {
                TestFixture.IPFSService.OnReceivedHello_ += Receiver;
                using TempNode otherNode = new TempNode();
                await otherNode.StartAsync();

                Peer self = await TestFixture.Ipfs.LocalPeer;
                MultiAddress[] addresses = self.Addresses.ToArray();

                Peer other = await otherNode.LocalPeer;

                await otherNode.Swarm.ConnectAsync(addresses[0]);
                using CancellationTokenSource cts = new(100);

                await otherNode.PubSub.PublishAsync("/X-Arteranos/Server-Hello", "hi", cts.Token);

                await TestFixture.WaitForConditionAsync(5, () => (sender != null), "Message was not received");

                Assert.AreEqual(sender.Id, other.Id);
            }
            finally
            {
                TestFixture.IPFSService.OnReceivedHello_ -= Receiver;
            }
        }

        [UnityTest]
        public IEnumerator SendServerHello()
        {
            yield return TestFixture.SetupIPFS();
            Assert.IsNotNull(TestFixture.Ipfs);

            Task.Run(SendServerHelloAsync).Wait();
        }

        public async Task SendServerHelloAsync()
        {
            Peer sender = null;

            using TempNode otherNode = new TempNode();
            await otherNode.StartAsync();

            Peer self = await TestFixture.Ipfs.LocalPeer;
            Peer other = await otherNode.LocalPeer;

            MultiAddress[] addresses = other.Addresses.ToArray();
            await TestFixture.Ipfs.Swarm.ConnectAsync(addresses[0]);

            using CancellationTokenSource cts = new(100);

            await otherNode.PubSub.SubscribeAsync("/X-Arteranos/Server-Hello", msg => sender = msg.Sender, cts.Token);

            await TestFixture.IPFSService.SendServerHello_();

            await TestFixture.WaitForConditionAsync(5, () => (sender != null), "Message was not received");

            Assert.AreEqual(sender.Id, self.Id);
        }
    }
}

#endif