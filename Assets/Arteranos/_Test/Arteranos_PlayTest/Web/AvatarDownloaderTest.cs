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
using Newtonsoft.Json;

using Object = UnityEngine.Object;
using System.Collections.Generic;

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

            yield return TestFixture.WaitForCondition(5, () => srv?.Ipfs_ != null, "IPFS server timeout");

            ipfs = srv.Ipfs_;

            self = Task.Run(async () => await ipfs.LocalPeer).Result;

            Task.Run(async () =>
            {
                Task t = UploadTestAvatar();

                await t;
            }).Wait();
        }

        Camera ca = null;
        Light li = null;
        GameObject pl = null;

        private void SetupScene()
        {
            ca = new GameObject("Camera").AddComponent<Camera>();
            ca.transform.position = new(0, 1, -2);

            li = new GameObject("Light").AddComponent<Light>();
            li.transform.SetPositionAndRotation(new(0, 3, 0), Quaternion.Euler(50, -30, 0));
            li.type = LightType.Directional;
            li.color = Color.white;

            GameObject bpl = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arteranos/_Test/Plane.prefab");
            pl = Object.Instantiate(bpl);
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
            go.transform.position = new(right ? 0.20f : -0.20f, 0);
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

            Object.Destroy(ca.gameObject);
            Object.Destroy(li.gameObject);
            Object.Destroy(pl);
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
            Assert.AreEqual("Head", am.Head.name);

            Assert.AreEqual(2, am.Eyes.Count);

            Assert.AreEqual(2, am.Feet.Count);

            Assert.IsTrue(am.Feet[0].Elevation > 0.126f);
            Assert.IsTrue(am.Feet[0].Elevation < 0.127f);

            Assert.IsNotNull(am.CenterEye);

            Assert.IsTrue(am.EyeHeight > 1.725f);
            Assert.IsTrue(am.EyeHeight < 1.726f);

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
                    InstallFootIKCollider = true,
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


        [UnityTest]
        public IEnumerator ScaleAvatar()
        {
            (AsyncOperationExecutor<Context> ao, Context co) =
                AvatarDownloader.PrepareDownloadAvatar(AvatarCid, new AvatarDownloaderOptions()
                {
                    DesiredHeight = 0.50f
                });

            Task t = ao.ExecuteAsync(co);

            while (!t.IsCompleted) yield return new WaitForEndOfFrame();

            GameObject avatar = AvatarDownloader.GetLoadedAvatar(co);
            avatar.SetActive(true);
            avatar.transform.rotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);

            IAvatarMeasures am = AvatarDownloader.GetAvatarMeasures(co);

            Assert.IsTrue(am.FullHeight > 0.499f);
            Assert.IsTrue(am.FullHeight < 0.501f);

            Assert.IsTrue(am.UnscaledHeight > 1.86f);
            Assert.IsTrue(am.UnscaledHeight < 1.87f);

            Transform transform = avatar.transform;

            Assert.IsTrue(transform.localScale.x < 0.27f);
            Assert.IsTrue(transform.localScale.y < 0.27f);
            Assert.IsTrue(transform.localScale.z < 0.27f);

            Assert.IsTrue(am.Feet[0].Elevation > 0.126f * transform.localScale.x);
            Assert.IsTrue(am.Feet[0].Elevation < 0.127f * transform.localScale.x);
        }

        [UnityTest]
        public IEnumerator AnimateAvatar()
        {
            (AsyncOperationExecutor<Context> ao, Context co) =
                AvatarDownloader.PrepareDownloadAvatar(AvatarCid, new AvatarDownloaderOptions()
                {
                    InstallAnimController = true
                });

            Task t = ao.ExecuteAsync(co);

            while (!t.IsCompleted) yield return new WaitForEndOfFrame();

            GameObject avatar = AvatarDownloader.GetLoadedAvatar(co);
            avatar.SetActive(true);
            avatar.transform.rotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);

            Animator animator = avatar.GetComponent<Animator>();

            Assert.IsNotNull(animator);

            UnityEngine.Avatar animAva = animator.avatar;

            Assert.IsTrue(animAva.isValid);
            Assert.IsTrue(animAva.isHuman);

            yield return new WaitForSeconds(2);

            animator.SetInteger("IntWalkFrontBack", 1);
            yield return new WaitForSeconds(5);

            animator.SetInteger("IntWalkFrontBack", 0);
            yield return new WaitForSeconds(2);

            animator.SetInteger("IntWalkFrontBack", -1);
            yield return new WaitForSeconds(5);

            animator.SetInteger("IntWalkFrontBack", 0);
            yield return new WaitForSeconds(2);

            animator.SetInteger("IntWalkLeftRight", 1);
            yield return new WaitForSeconds(5);

            animator.SetInteger("IntWalkLeftRight", 0);
            yield return new WaitForSeconds(2);

            animator.SetInteger("IntWalkLeftRight", -1);
            yield return new WaitForSeconds(5);

            animator.SetInteger("IntWalkLeftRight", 0);
            yield return new WaitForSeconds(2);
        }
        [UnityTest]
        public IEnumerator AnimateIKAvatar()
        {
            (AsyncOperationExecutor<Context> ao, Context co) =
                AvatarDownloader.PrepareDownloadAvatar(AvatarCid, new AvatarDownloaderOptions()
                {
                    InstallFootIK = true,
                    InstallFootIKCollider = true,
                    InstallAnimController = true
                });

            Task t = ao.ExecuteAsync(co);

            while (!t.IsCompleted) yield return new WaitForEndOfFrame();

            GameObject avatar = AvatarDownloader.GetLoadedAvatar(co);
            avatar.SetActive(true);
            avatar.transform.rotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);

            Animator animator = avatar.GetComponent<Animator>();

            Assert.IsNotNull(animator);

            animator.SetInteger("IntWalkFrontBack", 1);

            yield return new WaitForSeconds(5);
            GameObject go = CreateSteppingStone(false);
            yield return new WaitForSeconds(5);

            Object.Destroy(go);
            // yield return UnityPAK();
        }

        struct boneTranslation
        {
            public string BoneName;
            public string HumanName;
        }
        class table
        {
            public Dictionary<string, string> translationTable;
        }
        [UnityTest]
        public IEnumerator SaveBoneTranslationTable()
        {
            yield return null;

            const string femaleAvatarResource = "AvatarAnim/RPM_FemaleAvatar";

            UnityEngine.Avatar avatar_bp =
                Resources.Load<UnityEngine.Avatar>(femaleAvatarResource);


            table table = new() { translationTable = new() };
            foreach (HumanBone bone in avatar_bp.humanDescription.human)
                table.translationTable.Add(bone.boneName, bone.humanName);

            string json = JsonConvert.SerializeObject(table, Formatting.Indented);
            File.WriteAllText("RPMBoneTranslations.json", json);
        }
    }
}
