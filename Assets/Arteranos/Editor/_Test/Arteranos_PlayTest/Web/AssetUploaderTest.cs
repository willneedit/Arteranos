using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using Ipfs.Http;
using Arteranos.Services;
using System.IO;
using System.Threading.Tasks;
using Arteranos.Core;
using Ipfs;
using Ipfs.Unity;
using Arteranos.Web;
using Arteranos.Core.Operations;
using System.Linq;
using System;
using Object = UnityEngine.Object;

namespace Arteranos.PlayTest.Web
{
    public class AssetUploaderTest
    {
        private const string WebURLAsset = "https://github.com/willneedit/willneedit.github.io/raw/master/Abbey.zip";
        private const string PlainFileAsset = "Assets/Arteranos/Editor/_Test/Sceelix_Abbey.zip";
        private string FileURLAsset => $"file:///{PlainFileAsset}";
        private string QuotedFileAsset => $"\"{PlainFileAsset}\"";

        IPFSServiceImpl service = null;
        IpfsClientEx ipfs = null;

        [UnitySetUp]
        public IEnumerator Setup0()
        {
            TestFixtures.IPFSServiceFixture(ref service);

            yield return TestFixtures.StartIPFSAndWait(service);

            ipfs = service.Ipfs;
        }


        [UnityTest]
        public IEnumerator UploadLocalFile()
        {
            Cid AssetCid = null;

            try
            {
                (AsyncOperationExecutor<Context> ao, Context co) =
                    AssetUploader.PrepareUploadToIPFS(FileURLAsset, false);

                ao.ProgressChanged += (ratio, msg) => Debug.Log($"{ratio} - {msg}");

                yield return ao.ExecuteCoroutine(co);

                AssetCid = AssetUploader.GetUploadedCid(co);

                Assert.IsNotNull(AssetCid);

                Debug.Log($"{AssetCid}");
            }
            finally
            {
                if (AssetCid != null) ipfs.Pin.RemoveAsync(AssetCid).Wait();
            }
        }

        [UnityTest]
        public IEnumerator UploadNakedLocalFile()
        {
            Cid AssetCid = null;

            try
            {
                (AsyncOperationExecutor<Context> ao, Context co) =
                    AssetUploader.PrepareUploadToIPFS(PlainFileAsset, false);

                ao.ProgressChanged += (ratio, msg) => Debug.Log($"{ratio} - {msg}");

                yield return ao.ExecuteCoroutine(co);

                AssetCid = AssetUploader.GetUploadedCid(co);

                Assert.IsNotNull(AssetCid);

                Debug.Log($"{AssetCid}");
            }
            finally
            {
                if (AssetCid != null) ipfs.Pin.RemoveAsync(AssetCid).Wait();
            }
        }

        [UnityTest]
        public IEnumerator UploadQuotedLocalFile()
        {
            Cid AssetCid = null;

            try
            {
                (AsyncOperationExecutor<Context> ao, Context co) =
                    AssetUploader.PrepareUploadToIPFS(QuotedFileAsset, false);

                ao.ProgressChanged += (ratio, msg) => Debug.Log($"{ratio} - {msg}");

                yield return ao.ExecuteCoroutine(co);

                AssetCid = AssetUploader.GetUploadedCid(co);

                Assert.IsNotNull(AssetCid);

                Debug.Log($"{AssetCid}");
            }
            finally
            {
                if (AssetCid != null) ipfs.Pin.RemoveAsync(AssetCid).Wait();
            }
        }

        [UnityTest]
        public IEnumerator UploadWebFile()
        {
            Cid AssetCid = null;

            try
            {
                (AsyncOperationExecutor<Context> ao, Context co) =
                    AssetUploader.PrepareUploadToIPFS(WebURLAsset, false);

                ao.ProgressChanged += (ratio, msg) => Debug.Log($"{msg}");

                yield return ao.ExecuteCoroutine(co);

                AssetCid = AssetUploader.GetUploadedCid(co);

                Assert.IsNotNull(AssetCid);

            }
            finally
            {
                if (AssetCid != null) ipfs.Pin.RemoveAsync(AssetCid).Wait();
            }
        }

        [UnityTest]
        public IEnumerator UploadMissingFile()
        {
            Cid AssetCid = null;

            LogAssert.Expect(LogType.Exception, "FileNotFoundException: Could not find file 'C:\\DoesNotExist.no'.");

            try
            {
                (AsyncOperationExecutor<Context> ao, Context co) =
                    AssetUploader.PrepareUploadToIPFS(
                        "file:///C:/DoesNotExist.no", false);

                ao.ProgressChanged += (ratio, msg) => Debug.Log($"{msg}");

                Context returned = co;
                AggregateException ex = null;

                yield return ao.ExecuteCoroutine(co, (_ex, _co) =>
                {
                    ex = _ex;
                    returned = _co;
                });

                Assert.IsNull(returned);
                Assert.IsNotNull(ex);
            }
            finally
            {
                if (AssetCid != null) ipfs.Pin.RemoveAsync(AssetCid).Wait();
            }

        }

        [UnityTest]
        public IEnumerator UploadDirectory()
        {

            Cid AssetCid = null;

            try
            {
                (AsyncOperationExecutor<Context> ao, Context co) =
                    AssetUploader.PrepareUploadToIPFS(PlainFileAsset, true);

                ao.ProgressChanged += (ratio, msg) => Debug.Log($"{msg}");

                yield return ao.ExecuteCoroutine(co);

                AssetCid = AssetUploader.GetUploadedCid(co);

                Assert.IsNotNull(AssetCid);

                IFileSystemNode fsn = ipfs.FileSystem.ListAsync(AssetCid).Result;
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

                Cid resolved = null;
                yield return Asyncs.Async2Coroutine(IPFSService.ResolveToCid($"{AssetCid}/{files[0].Name}"), _r => resolved = _r);
                Assert.IsNotNull(resolved);

                IFileSystemNode fsn_AB = null;
                yield return Asyncs.Async2Coroutine(ipfs.FileSystem.ListAsync(resolved), _r => fsn_AB = _r);
                IFileSystemLink[] files_AB = fsn_AB.Links.ToArray();
                Assert.IsTrue(fsn_AB.IsDirectory);
                Assert.AreEqual(4, files_AB.Length);
                Assert.AreEqual("AssetBundles", files_AB[0].Name);
                Assert.AreEqual("AssetBundles.manifest", files_AB[1].Name);
                // ... and so on ...
            }
            finally
            {
                if (AssetCid != null) ipfs.Pin.RemoveAsync(AssetCid).Wait();
            }
        }
    }
}