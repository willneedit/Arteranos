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
using Arteranos.Core.Operations;

namespace Arteranos.PlayTest.Web
{
    public class WorldDownloaderTest
    {
        private const string PlainFileAsset = "Assets/Arteranos/_Test/Sceelix_Abbey.zip";
        private string FileURLAsset => $"file:///{PlainFileAsset}";

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

            srv = go1.AddComponent<IPFSServiceImpl>();

            yield return null;

            yield return TestFixture.WaitForCondition(5, () => srv?.Ipfs_ != null, "IPFS server timeout");

            ipfs = srv.Ipfs_;

            self = Task.Run(async () => await ipfs.LocalPeer).Result;

            Task.Run(async () => await UploadTestWorld()).Wait();
        }

        private async Task UploadTestWorld()
        {
            (AsyncOperationExecutor<Context> ao, Context co) =
                AssetUploader.PrepareUploadToIPFS(FileURLAsset);

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
                WorldInfo.DBDelete(WorldCid);
            }

            srv = null;
            ipfs = null;
            self = null;
            WorldCid = null;

            StartupManagerMock go1 = GameObject.FindObjectOfType<StartupManagerMock>();
            GameObject.Destroy(go1.gameObject);

            yield return new WaitForSeconds(1);
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

            WorldInfo wi = await WorldDownloader.GetWorldInfoAsync(co);

            Assert.IsNotNull(wi.WorldName);
            Assert.IsNotNull(wi.win.Author);
            Assert.AreEqual(WorldCid.ToString(), wi.WorldCid);

            UserID userID = wi.win.Author;
            Assert.AreEqual("Ancient Iwontsay", (string) userID);
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

            WorldInfo wi = await WorldDownloader.GetWorldInfoAsync(co);

            Assert.IsNotNull(wi);
            Assert.AreEqual(wi.WorldCid, WorldCid.ToString());

            Assert.IsNotEmpty(WorldDownloader.GetWorldCacheDir(WorldCid));
            Assert.IsTrue(Directory.Exists(WorldDownloader.GetWorldCacheDir(WorldCid)));

            Assert.IsNotEmpty(WorldDownloader.GetWorldABF(WorldCid));
            Assert.IsTrue(File.Exists(WorldDownloader.GetWorldABF(WorldCid)));

            // WorldInfo needs to be constant, even the the world was recently accessed.
            wi.Updated = DateTime.Now;


            string WICid = await wi.PublishAsync(true);

            Assert.IsNotNull(WICid);
            Assert.AreEqual(WICid, WorldDownloader.GetWorldInfoCid(co).ToString());
            Assert.AreEqual(WICid, wi.WorldInfoCid);

            // Look with the World Cid
            WorldInfo wi2 = WorldInfo.DBLookup(WorldCid);

            Assert.IsNotNull(wi2);
            Assert.AreEqual(wi2.WorldCid, WorldCid.ToString());
            Assert.AreEqual(wi2.WorldInfoCid, WICid);

            // Look up with the World Info Cid
            WorldInfo wi3 = await WorldInfo.RetrieveAsync(WICid);

            Assert.IsNotNull(wi3);
            Assert.AreEqual(wi3.WorldCid, WorldCid.ToString());
            Assert.AreEqual(wi3.WorldInfoCid, WICid);

        }
    }
}