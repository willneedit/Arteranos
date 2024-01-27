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
using Arteranos.Avatar;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Arteranos.PlayTest.Web
{
    public class AvatarDownloaderTest
    {
        private const string Asset_iws = "file:///Assets/Arteranos/_Test/Iwontsay.glb";


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
            Camera go = new GameObject("Camera").AddComponent<Camera>();
            go.transform.position = new(0, 1, -2);

            Light li = new GameObject("Light").AddComponent<Light>();
            li.transform.SetPositionAndRotation(new(0, 3, 0), Quaternion.Euler(50, -30, 0));
            li.type = LightType.Directional;
            li.color = Color.white;
        }

        private async Task UploadTestAvatar()
        {
            (AsyncOperationExecutor<Context> ao, Context co) =
                AssetUploader.PrepareUploadToIPFS(Asset_iws);

            await ao.ExecuteAsync(co);

            AvatarCid = AssetUploader.GetUploadedCid(co);

            Assert.IsNotNull(AvatarCid);
        }

        private GameObject CreateSteppingStone(bool right)
        {
            GameObject ob = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arteranos/_Test/SteppingStone.prefab");
            GameObject go = Object.Instantiate(ob);
            go.transform.position = new(right ? 0.15f : -0.15f, 0);
            return go;
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

            StartupManagerMock go1 = Object.FindObjectOfType<StartupManagerMock>();
            Object.Destroy(go1.gameObject);

            yield return null;

            Camera ca = Object.FindObjectOfType<Camera>();
            Object.Destroy(ca.gameObject);

            Light li = Object.FindObjectOfType<Light>();
            Object.Destroy(li.gameObject);

            yield return new WaitForSeconds(1);
        }

        public IEnumerator UnityPAK()
        {
            GameObject go = new("Delete me to continue!");

            while (go != null) { yield return new WaitForSeconds(1f); }
        }

        [UnityTest]
        public IEnumerator AvatarDownload()
        {
            Assert.IsNotNull(AvatarCid);

            (AsyncOperationExecutor<Context> ao, Context co) =
                AvatarDownloader.PrepareDownloadAvatar(AvatarCid);

            ao.ProgressChanged += (ratio, msg) => Debug.Log($"{ratio} - {msg}");

            Task t = ao.ExecuteAsync(co);

            while (!t.IsCompleted) yield return new WaitForEndOfFrame();

            Assert.IsNotNull(AvatarDownloader.GetLoadedAvatar(co));

            GameObject avatar = AvatarDownloader.GetLoadedAvatar(co);
            // avatar.SetActive(true);
            // avatar.transform.rotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);

            IObjectStats ar = AvatarDownloader.GetAvatarRating(co);
            Assert.AreEqual(1.0f, ar.Rating);
            Assert.IsTrue(ar.Vertices < 12000);
            Assert.IsTrue(ar.Triangles < 60000);
            Assert.IsTrue(ar.Vertices != 0);

            IAvatarMeasures am = AvatarDownloader.GetAvatarMeasures(co);

            Assert.IsNotNull(am);

            Assert.AreEqual("LeftHand", am.LeftHand.name);
            Assert.AreEqual("RightHand", am.RightHand.name);
            Assert.AreEqual("LeftFoot", am.LeftFoot.name);
            Assert.AreEqual("RightFoot", am.RightFoot.name);
            Assert.AreEqual("Head", am.Head.name);

            Assert.AreEqual(2, am.Eyes.Count);

            Assert.IsNotNull(am.CenterEye);

            Assert.IsTrue(am.EyeHeight > 1.725f);
            Assert.IsTrue(am.EyeHeight < 1.726f);

            Assert.IsTrue(am.FootElevation > 0.126f);
            Assert.IsTrue(am.FootElevation < 0.127f);

            Assert.IsTrue(am.FullHeight > 1.86f);
            Assert.IsTrue(am.FullHeight < 1.87f);

            Assert.AreEqual(1, am.MouthOpen.Count);
            Assert.IsNotNull(am.MouthOpen[0].Renderer);
            Assert.IsTrue(am.MouthOpen[0].Index >= 0);

            Assert.AreEqual(1, am.EyeBlinkLeft.Count);
            Assert.IsNotNull(am.EyeBlinkLeft[0].Renderer);
            Assert.IsTrue(am.EyeBlinkLeft[0].Index >= 0);

            Assert.AreEqual(1, am.EyeBlinkRight.Count);
            Assert.IsNotNull(am.EyeBlinkRight[0].Renderer);
            Assert.IsTrue(am.EyeBlinkRight[0].Index >= 0);


            Assert.IsNull(avatar.GetComponent<AvatarEyeAnimator>());

            // yield return new WaitForSeconds(5);
            // yield return UnityPAK();
        }

        [UnityTest]
        public IEnumerator InstallAvatarEyeAnimator()
        {
            (AsyncOperationExecutor<Context> ao, Context co) =
                AvatarDownloader.PrepareDownloadAvatar(AvatarCid, new AvatarDownloaderOptions()
                {
                    InstallEyeAnimation = true
                });

            Task t = ao.ExecuteAsync(co);

            while (!t.IsCompleted) yield return new WaitForEndOfFrame();

            GameObject avatar = AvatarDownloader.GetLoadedAvatar(co);
            avatar.SetActive(true);

            Assert.IsNotNull(avatar.GetComponent<AvatarEyeAnimator>());

            // yield return UnityPAK();
        }

        [UnityTest]
        public IEnumerator InstallIK()
        {
            (AsyncOperationExecutor<Context> ao, Context co) =
                AvatarDownloader.PrepareDownloadAvatar(AvatarCid, new AvatarDownloaderOptions()
                {
                    InstallFootIK = true,
                    InstallHandIK = true
                });

            Task t = ao.ExecuteAsync(co);

            while (!t.IsCompleted) yield return new WaitForEndOfFrame();

            GameObject avatar = AvatarDownloader.GetLoadedAvatar(co);
            avatar.SetActive(true);

            IAvatarMeasures am = AvatarDownloader.GetAvatarMeasures(co);

            Assert.AreEqual(12, am.JointNames.Count);
            // yield return UnityPAK();
        }

        [UnityTest]
        public IEnumerator TestFootIK()
        {
            (AsyncOperationExecutor<Context> ao, Context co) =
                AvatarDownloader.PrepareDownloadAvatar(AvatarCid, new AvatarDownloaderOptions()
                {
                    InstallFootIK = true,
                    InstallHandIK = true
                });

            Task t = ao.ExecuteAsync(co);

            while (!t.IsCompleted) yield return new WaitForEndOfFrame();

            GameObject avatar = AvatarDownloader.GetLoadedAvatar(co);
            avatar.SetActive(true);
            avatar.transform.rotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);

            GameObject go = CreateSteppingStone(false);
            yield return new WaitForSeconds(5);

            Object.Destroy(go);
            go = CreateSteppingStone(true);
            yield return new WaitForSeconds(5);

            Object.Destroy(go);
        }

    }
}
