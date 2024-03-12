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
using System.Linq;

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

        [UnitySetUp]
        public IEnumerator SetupIPFS()
        {
            GameObject go1 = TestFixture.SetupStartupManagerMock();

            yield return null;

            srv = go1.AddComponent<IPFSServiceImpl>();

            yield return null;

            yield return TestFixture.WaitForCondition(5, () => srv?.Ipfs_ != null, "IPFS server timeout");

            ipfs = srv.Ipfs_;

        }

        [UnityTearDown]
        public IEnumerator TeardownIPFS()
        {
            srv = null;
            ipfs = null;

            StartupManagerMock go1 = Object.FindObjectOfType<StartupManagerMock>();
            Object.Destroy(go1.gameObject);

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
                    AssetUploader.PrepareUploadToIPFS(FileURLAsset, false);

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
                    AssetUploader.PrepareUploadToIPFS(PlainFileAsset, false);

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
                    AssetUploader.PrepareUploadToIPFS(QuotedFileAsset, false);

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
                    AssetUploader.PrepareUploadToIPFS(WebURLAsset, false);

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
                        "file:///C:/DoesNotExist.no", false);

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

        [UnityTest]
        public IEnumerator UploadDirectory()
        {
            yield return null;

            Task.Run(UploadDirectoryAsync).Wait();

            yield return null;
        }

        public async Task UploadDirectoryAsync()
        {

            Cid AssetCid = null;

            try
            {
                (AsyncOperationExecutor<Context> ao, Context co) =
                    AssetUploader.PrepareUploadToIPFS(PlainFileAsset, true);

                ao.ProgressChanged += (ratio, msg) => Debug.Log($"{msg}");

                await ao.ExecuteAsync(co);

                AssetCid = AssetUploader.GetUploadedCid(co);

                Assert.IsNotNull(AssetCid);

                IFileSystemNode fsn = await ipfs.FileSystem.ListFileAsync(AssetCid);
                IFileSystemLink[] files = fsn.Links.ToArray();

                Assert.IsTrue(fsn.IsDirectory);
                Assert.AreEqual(3, files.Length);
                Assert.AreEqual("AssetBundles", files[0].Name);
                Assert.AreEqual("Metadata.json", files[1].Name);
                Assert.AreEqual("Screenshot.png", files[2].Name);
                Assert.AreEqual(0, files[0].Size);
                Assert.AreNotEqual(0, files[1].Size);
                Assert.AreNotEqual(0, files[2].Size);

                // How to search for a specific file in an archive: ListFileAsync, then iterate
                // Alternatively, using file[0].Id works as well.
                IFileSystemNode fsn_AB = await ipfs.FileSystem.ListFileAsync($"{AssetCid}/{files[0].Name}");
                IFileSystemLink[] files_AB = fsn_AB.Links.ToArray();
                Assert.IsTrue(fsn_AB.IsDirectory);
                Assert.AreEqual(4, files_AB.Length);
                Assert.AreEqual("AssetBundles", files_AB[0].Name);
                Assert.AreEqual("AssetBundles.manifest", files_AB[1].Name);
                // ... and so on ...
            }
            finally
            {
                if (AssetCid != null) await ipfs.Block.RemoveAsync(AssetCid);
            }
        }
    }
}