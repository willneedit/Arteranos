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
using Arteranos.Core.Cryptography;
using System.Security.Cryptography;
using Arteranos.UI;
using Arteranos.Web;

namespace Arteranos.PlayTest.Web
{
    public class WorldDownloaderTest
    {
        IPFSServiceImpl srv = null;
        IpfsEngine ipfs = null;
        Peer self = null;

        Cid WorldCid = null;

        [UnitySetUp]
        public IEnumerator SetupIPFS()
        {
            GameObject go1 = new("SettingsManager");
            StartupManagerMock sm = go1.AddComponent<StartupManagerMock>();

            yield return null;

            GameObject go2 = new("IPFS Service");
            srv = go2.AddComponent<IPFSServiceImpl>();

            yield return null;

            yield return TestFixture.WaitForCondition(5, () => srv?._Ipfs != null, "IPFS server timeout");

            ipfs = srv._Ipfs;

            self = Task.Run(async () => await ipfs.LocalPeer).Result;

            Task.Run(async () => await UploadTestWorld()).Wait();
        }

        private async Task UploadTestWorld()
        {
            (AsyncOperationExecutor<Context> ao, Context co) =
                AssetUploader.PrepareUploadToIPFS(
                    "file:///D:/Users/carsten/Documents/Sceelix_Abbey.zip");

            await ao.ExecuteAsync(co);

            WorldCid = AssetUploader.GetUploadedCid(co);

            Assert.IsNotNull(WorldCid);
        }

        [UnityTearDown]
        public IEnumerator TeardownIPFS()
        {
            if (WorldCid != null)
            {
                ipfs.Block.RemoveAsync(WorldCid).Wait();
            }

            srv = null;
            ipfs = null;
            self = null;
            WorldCid = null;

            StartupManagerMock go1 = GameObject.FindObjectOfType<StartupManagerMock>();
            GameObject.Destroy(go1.gameObject);

            var go2 = GameObject.FindObjectOfType<IPFSServiceImpl>();
            GameObject.Destroy(go2.gameObject);

            yield return null;
        }

        [Test]
        public void DownloadWorld()
        {
            Task.Run(DownloadWorldAsync).Wait();
        }

        public async Task DownloadWorldAsync()
        {
            Assert.IsNotNull(WorldCid);

            (AsyncOperationExecutor<Context> ao, Context co) =
                WorldDownloader.PrepareDownloadWorld(WorldCid);

            ao.ProgressChanged += (ratio, msg) => Debug.Log($"{ratio} - {msg}");

            await ao.ExecuteAsync(co);

            WorldInfo wi = WorldDownloader.GetWorldInfo(co);

            Assert.IsNotNull(wi.WorldName);
            Assert.IsNotNull(wi.AuthorNickname);
            Assert.AreEqual(WorldCid.ToString(), wi.WorldCid);
        }

        [Test]
        public void GetWorldData()
        {
            Task.Run(GetWorldDataAsync).Wait();
        }

        public async Task GetWorldDataAsync()
        {
            (AsyncOperationExecutor<Context> ao, Context co) =
                WorldDownloader.PrepareDownloadWorld(WorldCid);

            await ao.ExecuteAsync(co);

            WorldInfo wi = WorldDownloader.GetWorldInfo(co);

            Assert.IsNotNull(wi);
            Assert.AreEqual(wi.WorldCid, WorldCid.ToString());

            Assert.IsNotEmpty(WorldDownloader.GetWorldCacheDir(WorldCid));
            Assert.IsTrue(Directory.Exists(WorldDownloader.GetWorldCacheDir(WorldCid)));

            Assert.IsNotEmpty(WorldDownloader.GetWorldABF(WorldCid));
            Assert.IsTrue(File.Exists(WorldDownloader.GetWorldABF(WorldCid)));

            // WorldInfo needs to be constant, even the the world was recently accessed.
            wi.Updated = DateTime.Now;

            using MemoryStream ms = new();
            wi.Serialize(ms);
            ms.Position = 0;
            IFileSystemNode fsn = await ipfs.FileSystem.AddAsync(ms, "", new() { OnlyHash = true });
            Assert.IsNotNull(fsn);

            Cid WICid = fsn.Id;

            Debug.Log($"{WICid}");
        }
    }
}