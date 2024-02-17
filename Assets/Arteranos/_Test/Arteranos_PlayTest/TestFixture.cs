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
using UnityEditor;

namespace Arteranos.PlayTest
{
    public class TestFixture
    {
        private static IpfsEngine ipfs = null;
        private static IPFSServiceImpl iPFSService = null;

        public static IpfsEngine Ipfs { get => ipfs; }
        public static IPFSServiceImpl IPFSService { get => iPFSService; }

        public static GameObject SetupStartupManagerMock()
        {
            GameObject bp = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arteranos/_Test/Arteranos_PlayTest/SceneEssentialsMock.prefab");
            return UnityEngine.Object.Instantiate(bp);
        }
        public static IEnumerator SetupIPFS()
        {
            ipfs = null;
            iPFSService = null;

            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            IPFSServiceImpl old = GameObject.FindObjectOfType<IPFSServiceImpl>();
            if(old != null && old.Ipfs_ != null)
            {
                ipfs = old.Ipfs_;
                Debug.LogWarning("Reusing old service");
                yield break;
            }

            GameObject go = new("IPFS Service");
            IPFSServiceImpl srv = go.AddComponent<IPFSServiceImpl>();

            DateTime expiry = DateTime.Now + TimeSpan.FromSeconds(5);
            while (srv.Ipfs_ == null)
            {
                if (expiry < DateTime.Now) Assert.Fail("Timeout when setting up IPFS backend");
                yield return new WaitForEndOfFrame();
            }

            ipfs = srv.Ipfs_;
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
