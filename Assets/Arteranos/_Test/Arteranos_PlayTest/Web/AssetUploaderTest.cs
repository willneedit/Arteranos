using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using Ipfs.Engine;
using Arteranos.Services;
using System.IO;
using System.Threading.Tasks;
using Arteranos.Core;
using Ipfs;
using Arteranos.Web;
using Arteranos.Core.Operations;

namespace Arteranos.PlayTest.Web
{
    public class AssetUploaderTest
    {
        private const string WebURLAsset = "https://github.com/willneedit/willneedit.github.io/raw/master/Abbey.zip";
        private const string PlainFileAsset = "Assets/Arteranos/_Test/Sceelix_Abbey.zip";
        private string FileURLAsset => $"file:///{PlainFileAsset}";
        private string QuotedFileAsset => $"\"{PlainFileAsset}\"";

        IPFSServiceImpl srv = null;
        IpfsEngine ipfs = null;
        Peer self = null;

        [UnitySetUp]
        public IEnumerator SetupIPFS()
        {
            GameObject go1 = TestFixture.SetupStartupManagerMock();

            yield return null;

            srv = go1.AddComponent<IPFSServiceImpl>();

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

            StartupManagerMock go1 = GameObject.FindObjectOfType<StartupManagerMock>();
            GameObject.Destroy(go1.gameObject);

            yield return new WaitForSeconds(1);
        }

        [UnityTest]
        public IEnumerator UploadLocalFile()
        {
            yield return null;

            Task.Run(UploadLocalFileAsync).Wait();

            yield return null;
        }

        public async Task UploadLocalFileAsync()
        {
            Cid AssetCid = null;

            try
            {
                (AsyncOperationExecutor<Context> ao, Context co) =
                    AssetUploader.PrepareUploadToIPFS(FileURLAsset);

                ao.ProgressChanged += (ratio, msg) => Debug.Log($"{ratio} - {msg}");

                await ao.ExecuteAsync(co);

                AssetCid = AssetUploader.GetUploadedCid(co);

                Assert.IsNotNull(AssetCid);

                Debug.Log($"{AssetCid}");
            }
            finally
            {
                if (AssetCid != null) await ipfs.Block.RemoveAsync(AssetCid);
            }
        }

        [UnityTest]
        public IEnumerator UploadNakedLocalFile()
        {
            yield return null;

            Task.Run(UploadNakedLocalFileAsync).Wait();

            yield return null;
        }

        public async Task UploadNakedLocalFileAsync()
        {
            Cid AssetCid = null;

            try
            {
                (AsyncOperationExecutor<Context> ao, Context co) =
                    AssetUploader.PrepareUploadToIPFS(PlainFileAsset);

                ao.ProgressChanged += (ratio, msg) => Debug.Log($"{ratio} - {msg}");

                await ao.ExecuteAsync(co);

                AssetCid = AssetUploader.GetUploadedCid(co);

                Assert.IsNotNull(AssetCid);

                Debug.Log($"{AssetCid}");
            }
            finally
            {
                if (AssetCid != null) await ipfs.Block.RemoveAsync(AssetCid);
            }
        }

        [UnityTest]
        public IEnumerator UploadQuotedLocalFile()
        {
            yield return null;

            Task.Run(UploadQuotedLocalFileAsync).Wait();

            yield return null;
        }

        public async Task UploadQuotedLocalFileAsync()
        {
            Cid AssetCid = null;

            try
            {
                (AsyncOperationExecutor<Context> ao, Context co) =
                    AssetUploader.PrepareUploadToIPFS(QuotedFileAsset);

                ao.ProgressChanged += (ratio, msg) => Debug.Log($"{ratio} - {msg}");

                await ao.ExecuteAsync(co);

                AssetCid = AssetUploader.GetUploadedCid(co);

                Assert.IsNotNull(AssetCid);

                Debug.Log($"{AssetCid}");
            }
            finally
            {
                if (AssetCid != null) await ipfs.Block.RemoveAsync(AssetCid);
            }
        }

        [UnityTest]
        public IEnumerator UploadWebFile()
        {
            yield return null;

            Task.Run(UploadWebFileAsync).Wait();

            yield return null;
        }

        public async Task UploadWebFileAsync()
        {
            Cid AssetCid = null;

            try
            {
                (AsyncOperationExecutor<Context> ao, Context co) =
                    AssetUploader.PrepareUploadToIPFS(WebURLAsset);

                ao.ProgressChanged += (ratio, msg) => Debug.Log($"{msg}");

                await ao.ExecuteAsync(co);

                AssetCid = AssetUploader.GetUploadedCid(co);

                Assert.IsNotNull(AssetCid);

            }
            finally
            {
                if (AssetCid != null) await ipfs.Block.RemoveAsync(AssetCid);
            }
        }

        [UnityTest]
        public IEnumerator UploadMissingFile()
        {
            yield return null;

            Task.Run(UploadMissingFileAsync).Wait();

            yield return null;
        }

        public async Task UploadMissingFileAsync()
        {
            Cid AssetCid = null;

            LogAssert.Expect(LogType.Exception, "FileNotFoundException: Could not find file 'C:\\DoesNotExist.no'.");

            try
            {
                (AsyncOperationExecutor<Context> ao, Context co) =
                    AssetUploader.PrepareUploadToIPFS(
                        "file:///C:/DoesNotExist.no");

                ao.ProgressChanged += (ratio, msg) => Debug.Log($"{msg}");

                await ao.ExecuteAsync(co);

                Assert.Fail("Did not throw");

                AssetCid = AssetUploader.GetUploadedCid(co);

                Assert.IsNotNull(AssetCid);

            }
            catch (FileNotFoundException) { }
            finally
            {
                if (AssetCid != null) await ipfs.Block.RemoveAsync(AssetCid);
            }

        }
    }
}