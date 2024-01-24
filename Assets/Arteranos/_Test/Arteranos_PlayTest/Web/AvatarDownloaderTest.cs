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
using System;

namespace Arteranos.PlayTest.Web
{
    public class AvatarDownloaderTest
    {
        private const string PlainFileAsset = "Assets/Arteranos/_Test/Iwontsay.glb";
        private string FileURLAsset => $"file:///{PlainFileAsset}";
        private string QuotedFileAsset => $"\"{PlainFileAsset}\"";


        IPFSServiceImpl srv = null;
        IpfsEngine ipfs = null;
        Peer self = null;

        Cid AvatarCid = null;


        [UnitySetUp]
        public IEnumerator SetupIPFS()
        {
            SetupScene();

            GameObject go1 = new("SettingsManager");
            StartupManagerMock sm = go1.AddComponent<StartupManagerMock>();

            yield return null;

            srv = go1.AddComponent<IPFSServiceImpl>();

            yield return null;

            yield return TestFixture.WaitForCondition(5, () => srv?._Ipfs != null, "IPFS server timeout");

            ipfs = srv._Ipfs;

            self = Task.Run(async () => await ipfs.LocalPeer).Result;

            Task.Run(async () =>
            {
                Task t = UploadTestAvatar();

                await t;
            }).Wait();
        }

        private void SetupScene()
        {
            GameObject go = new("Camera");
            go.AddComponent<Camera>();
            go.transform.position = new(0, 1.75f, -2);

            GameObject go2 = new("Light");
            Light li = go2.AddComponent<Light>();
            go2.transform.position = new(0, 3, 0);
            go2.transform.rotation = Quaternion.Euler(50, -30, 0);
            li.type = LightType.Directional;
            li.color = Color.white;
        }

        private async Task UploadTestAvatar()
        {
            (AsyncOperationExecutor<Context> ao, Context co) =
                AssetUploader.PrepareUploadToIPFS(FileURLAsset);

            await ao.ExecuteAsync(co);

            AvatarCid = AssetUploader.GetUploadedCid(co);

            Assert.IsNotNull(AvatarCid);
        }

        [UnityTearDown]
        public IEnumerator TeardownIPFS()
        {
            if (AvatarCid != null)
            {
                ipfs.Block.RemoveAsync(AvatarCid).Wait();
                WorldInfo.DBDelete(AvatarCid);
            }

            srv = null;
            ipfs = null;
            self = null;
            AvatarCid = null;

            StartupManagerMock go1 = GameObject.FindObjectOfType<StartupManagerMock>();
            GameObject.Destroy(go1.gameObject);

            yield return null;

            Camera ca = GameObject.FindObjectOfType<Camera>();
            GameObject.Destroy(ca.gameObject);

            Light li = GameObject.FindObjectOfType<Light>();
            GameObject.Destroy(li.gameObject);
        }

        public IEnumerator UnityPAK()
        {
            GameObject go = new("Delete me to continue!");

            while (go != null) { yield return new WaitForSeconds(1f); }
        }

        [UnityTest]
        public IEnumerator AvatarDownloadAsync()
        {
            Assert.IsNotNull(AvatarCid);

            (AsyncOperationExecutor<Context> ao, Context co) =
                AvatarDownloader.PrepareDownloadAvatar(AvatarCid);

            ao.ProgressChanged += (ratio, msg) => Debug.Log($"{ratio} - {msg}");

            Task t = ao.ExecuteAsync(co);

            while (!t.IsCompleted) yield return new WaitForEndOfFrame();

            Assert.IsNotNull(AvatarDownloader.GetLoadedAvatar(co));

            GameObject avatar = AvatarDownloader.GetLoadedAvatar(co);
            avatar.SetActive(true);
            avatar.transform.rotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);

            yield return new WaitForSeconds(5);
        }
    }
}
