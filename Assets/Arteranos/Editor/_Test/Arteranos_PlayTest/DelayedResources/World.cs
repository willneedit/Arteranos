using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

using Arteranos.Services;
using Arteranos.Core;
using Ipfs;
using Arteranos.Core.Operations;
using Arteranos.Core.Managed;
using AssetBundle = Arteranos.Core.Managed.AssetBundle;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace Arteranos.PlayTest.DelayedResources
{
    public class WorldTest
    {
        private const string PlainFileAsset = "Assets/Arteranos/Editor/_Test/Sceelix_Abbey.zip";
        private string FileURLAsset => $"file:///{PlainFileAsset}";
        Cid WorldCid = null;


        IPFSService service = null;

        [UnitySetUp]
        public IEnumerator Setup0()
        {
            TestFixtures.IPFSServiceFixture(ref service);

            yield return TestFixtures.StartIPFSAndWait(service);

            yield return UploadTestWorld();
        }

        [UnityTearDown]
        public IEnumerator TearDown0()
        {
            G.XRVisualConfigurator.StartFading(0, 0);
            yield return null;
        }

        private IEnumerator UploadTestWorld()
        {
            (AsyncOperationExecutor<Context> ao, Context co) =
                AssetUploader.PrepareUploadToIPFS(FileURLAsset, true);

            yield return ao.ExecuteCoroutine(co);

            WorldCid = AssetUploader.GetUploadedCid(co);

            Assert.IsNotNull(WorldCid);
        }

        [UnityTest]
        public IEnumerator T001_QuickInit()
        {
            Stopwatch sw = Stopwatch.StartNew();
            World world = new(WorldCid);
            Assert.IsTrue(sw.ElapsedMilliseconds < 2);
            Assert.IsNotNull(world);

            yield return null;
        }

        [UnityTest]
        public IEnumerator T002_IsNotFullWorls()
        {
            World world = new(WorldCid);

            Stopwatch sw = Stopwatch.StartNew();
            yield return world.TemplateCid.WaitFor();
            yield return world.DecorationCid.WaitFor();
            Debug.Log($"{sw.ElapsedMilliseconds} ms");

            sw.Restart();
            Cid tcid = world.TemplateCid;
            Cid dcid = world.DecorationCid;
            Debug.Log($"{sw.ElapsedMilliseconds} ms");

            Assert.IsNotNull(tcid);
            Assert.IsNull(dcid);
        }

        [UnityTest]
        public IEnumerator T003_GetInfos()
        {
            World world = new(WorldCid);

            Stopwatch sw = Stopwatch.StartNew();
            yield return world.TemplateInfo.WaitFor();
            yield return world.WorldInfo.WaitFor();
            Debug.Log($"{sw.ElapsedMilliseconds} ms");

            sw.Restart();
            Assert.AreEqual("Sceelix Abbey", world.WorldInfo.Result.WorldName);
            Debug.Log($"{sw.ElapsedMilliseconds} ms");
        }

        [UnityTest]
        public IEnumerator T004_ScreenshotPNG() 
        {
            World world = new(WorldCid);

            Stopwatch sw = Stopwatch.StartNew();
            yield return world.ScreenshotPNG.WaitFor();
            Debug.Log($"{sw.ElapsedMilliseconds} ms");

            byte[] screenshotPNG = world.ScreenshotPNG;
            Assert.IsNotNull(screenshotPNG);
        }

        [UnityTest]
        public IEnumerator T005_Content()
        {
            static void ReportProgress(long bytes, long total)
            {
                Debug.Log($"{bytes} out of {total}");
            }

            World world = new(WorldCid);
            world.OnReportingProgress += ReportProgress;

            Stopwatch sw = Stopwatch.StartNew();
            yield return world.TemplateContent.WaitFor();
            Debug.Log($"{sw.ElapsedMilliseconds} ms");

            sw.Restart();
            AssetBundle assetBundle = world.TemplateContent;
            Debug.Log($"{sw.ElapsedMilliseconds} ms");

            Assert.IsNotNull(assetBundle);
            Assert.IsNotNull((UnityEngine.AssetBundle)assetBundle);
            assetBundle.Dispose();
        }
    }
 }