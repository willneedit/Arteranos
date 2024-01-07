using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using Ipfs.Engine;
using Arteranos.Services;
using System;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using Arteranos.Core;
using Ipfs;
using System.Linq;
using System.Threading;

namespace Arteranos.PlayTest.Services
{
    public class IPFS
    {
        IPFSService srv = null;
        IpfsEngine ipfs = null;
        Peer self = null;

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

            try
            {
                srv.OnReceivedHello += Receiver;
                using TempNode otherNode = new TempNode();
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
            }
        }
    }
}