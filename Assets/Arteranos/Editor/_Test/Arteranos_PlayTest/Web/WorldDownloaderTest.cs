using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using Ipfs.Http;
using Arteranos.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using Arteranos.Core;
using Ipfs;
using Arteranos.Core.Operations;
using System.Diagnostics;
using UnityEditor;

namespace Arteranos.PlayTest.Web
{
    public class WorldDownloaderTest
    {
        private const string PlainFileAsset = "Assets/Arteranos/_Test/Sceelix_Abbey.zip";
        private string FileURLAsset => $"file:///{PlainFileAsset}";

        IPFSServiceImpl srv = null;
        IpfsClientEx ipfs = null;

        Cid WorldCid = null;

        [UnitySetUp]
        public IEnumerator SetupIPFS()
        {
            GameObject go1 = TestFixtures.SetupStartupManagerMock();

            yield return null;

            srv = go1.AddComponent<IPFSServiceImpl>();

            yield return null;

            yield return TestFixtures.WaitForCondition(5, () => srv && srv.Ipfs_ != null, "IPFS server timeout");

            ipfs = srv.Ipfs_;

            yield return UploadTestWorld();
        }

        private IEnumerator UploadTestWorld()
        {
            (AsyncOperationExecutor<Context> ao, Context co) =
                AssetUploader.PrepareUploadToIPFS(FileURLAsset, true);

            yield return ao.ExecuteCoroutine(co);

            WorldCid = AssetUploader.GetUploadedCid(co);

            Assert.IsNotNull(WorldCid);
        }

        [UnityTearDown]
        public IEnumerator TeardownIPFS()
        {
            if (WorldCid != null)
            {
                ipfs.Pin.RemoveAsync(WorldCid).Wait();
                WorldInfo.DBDelete(WorldCid);
            }

            srv = null;
            ipfs = null;
            WorldCid = null;

            StartupManagerMock go1 = UnityEngine.Object.FindObjectOfType<StartupManagerMock>();
            UnityEngine.Object.Destroy(go1.gameObject);

            yield return new WaitForSeconds(1);
        }

        [UnityTest]
        public IEnumerator DownloadWorld()
        {
            Stopwatch sw = Stopwatch.StartNew();
            yield return DownloadWorldCoroutine();
            sw.Stop();
            UnityEngine.Debug.Log($"DownloadWorld elapsed time: {sw.Elapsed.Milliseconds} milliseconds");
        }

        public IEnumerator DownloadWorldCoroutine()
        {
            (AsyncOperationExecutor<Context> ao, Context co) =
                WorldDownloader.PrepareGetWorldTemplate(WorldCid);

            ao.ProgressChanged += (ratio, msg) => UnityEngine.Debug.Log($"{ratio} - {msg}");

            yield return ao.ExecuteCoroutine(co);

            string file = WorldDownloader.GetWorldDataFile(co);

            Assert.IsNotNull(file);
            Assert.IsTrue(File.Exists(file));
        }

        [UnityTest]
        public IEnumerator GetWorldInfo()
        {
            Stopwatch sw = Stopwatch.StartNew();
            yield return GetWorldInfoCoroutine();
            sw.Stop();
            UnityEngine.Debug.Log($"GetWorldInfo elapsed time: {sw.Elapsed.Milliseconds} milliseconds");
        }

        public IEnumerator GetWorldInfoCoroutine()
        {
            (AsyncOperationExecutor<Context> ao, Context co) =
                WorldDownloader.PrepareGetWorldInfo(WorldCid);

            yield return ao.ExecuteCoroutine(co);

            WorldInfo wi = WorldDownloader.GetWorldInfo(co);

            Assert.IsNotNull(wi);
            Assert.IsNotNull(wi.win.ScreenshotPNG);

            Assert.IsNotNull(wi.WorldName);
            Assert.IsNotNull(wi.win.Author);

            UserID userID = wi.win.Author;
            Assert.AreEqual("Ancient Iwontsay", (string)userID);

            Stream stream = File.OpenRead("Assets/Arteranos/_Test/Screenshot.png");
            byte[] data = new byte[stream.Length];
            stream.Read(data, 0, data.Length);
            long length = data.Length < stream.Length ? data.Length : stream.Length;

            if (wi.win.ScreenshotPNG.Length != stream.Length)
               UnityEngine.Debug.Log($"Screenshot length mismatch, original={data.Length}, retrieved={wi.win.ScreenshotPNG.Length}");

            for (long i = 0; i < length; i++)
                if (data[i] != wi.win.ScreenshotPNG[i])
                    Assert.Fail($"Screenshot doesn't match: offset={i}, original={data[i]}, retrieved={wi.win.ScreenshotPNG[i]}");

        }
    }
}