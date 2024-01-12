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

namespace Arteranos.PlayTest
{
    public class TestFixture
    {
        private static IpfsEngine ipfs = null;
        private static IPFSServiceImpl iPFSService = null;

        public static IpfsEngine Ipfs { get => ipfs; }
        public static IPFSServiceImpl IPFSService { get => iPFSService; }

        public static IEnumerator SetupIPFS()
        {
            ipfs = null;
            iPFSService = null;

            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            IPFSServiceImpl old = GameObject.FindObjectOfType<IPFSServiceImpl>();
            if(old != null && old.Ipfs != null)
            {
                ipfs = old.Ipfs;
                Debug.LogWarning("Reusing old service");
                yield break;
            }

            GameObject go = new GameObject("IPFS Service");
            IPFSServiceImpl srv = go.AddComponent<IPFSServiceImpl>();

            DateTime expiry = DateTime.Now + TimeSpan.FromSeconds(5);
            while (srv.Ipfs == null)
            {
                if (expiry < DateTime.Now) Assert.Fail("Timeout when setting up IPFS backend");
                yield return new WaitForEndOfFrame();
            }

            ipfs = srv.Ipfs;
            iPFSService = srv;
        }

        public static IEnumerator EnsureIPFSStopped()
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            IPFSServiceImpl old = GameObject.FindObjectOfType<IPFSServiceImpl>();
            if (old != null) GameObject.Destroy(old.gameObject);
            yield return new WaitForSeconds(1);
        }

        public static IEnumerator WaitForCondition(int timeoutSeconds, Func<bool> condition, string message)
        {
            DateTime expiry = DateTime.Now + TimeSpan.FromSeconds(timeoutSeconds);
            while(!condition())
            {
                if (expiry < DateTime.Now) Assert.Fail(message);
                yield return new WaitForEndOfFrame();
            }
        }

        public static async Task WaitForConditionAsync(int timeoutSeconds, Func<bool> condition, string message)
        {
            DateTime expiry = DateTime.Now + TimeSpan.FromSeconds(timeoutSeconds);
            while (!condition())
            {
                if (expiry < DateTime.Now)
                {
                    Debug.LogError(message);
                    Assert.Fail(message);
                }
                await Task.Yield();
            }
        }
    }

    public class StartupManagerMock : SettingsManager
    {
        protected override void Awake()
        {
            Instance = this;

            base.Awake();
        }

        protected override bool IsSelf_(MultiHash ServerPeerID)
        {
            throw new NotImplementedException();
        }

        protected override void OnDestroy()
        {
            Instance = null;
        }

        protected override void PingServerChangeWorld_(string invoker, string worldURL)
        {
            throw new NotImplementedException();
        }

        protected override void StartCoroutineAsync_(Func<IEnumerator> action)
        {
            throw new NotImplementedException();
        }
    }

    class TempNode : IpfsEngine
    {
        static int nodeNumber;

        public TempNode(int port = 0)
            : base("xyzzy".ToCharArray())
        {
            Options.Repository.Folder = Path.Combine(Path.GetTempPath(), $"ipfs-{nodeNumber++}");
            Options.KeyChain.DefaultKeyType = "ed25519";

            Config.SetAsync(
                "Addresses.Swarm",
                JToken.FromObject(new string[] { $"/ip4/0.0.0.0/tcp/{port}" })
            ).Wait();

            Options.Discovery.DisableMdns = true;
            Options.Swarm.MinConnections = 0;
            Options.Swarm.PrivateNetworkKey = null;
            Options.Discovery.BootstrapPeers = new Ipfs.MultiAddress[0];
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (Directory.Exists(Options.Repository.Folder))
            {
                Directory.Delete(Options.Repository.Folder, true);
            }
        }
    }


}
