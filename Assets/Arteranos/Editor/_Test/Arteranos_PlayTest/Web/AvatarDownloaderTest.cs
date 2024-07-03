using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using Arteranos.Services;
using Arteranos.Core;
using Ipfs;
using Arteranos.Core.Operations;
using Arteranos.Avatar;

using Object = UnityEngine.Object;
using System.Collections.Generic;

namespace Arteranos.PlayTest.Web
{
    public class AvatarDownloaderTest
    {
        private const string Asset_iws = "file:///Assets/Arteranos/Editor/_Test/6394c1e69ef842b3a5112221.glb";


        IPFSServiceImpl service = null;

        Cid AvatarCid = null;


        [UnitySetUp]
        public IEnumerator Setup0()
        {
            TestFixtures.SceneFixture(ref ca, ref li, ref pl);
            ca.transform.localPosition = new Vector3(0, 1.75f, -3.5f);

            TestFixtures.IPFSServiceFixture(ref service);

            yield return TestFixtures.StartIPFSAndWait(service);

            yield return UploadTestAvatar();
        }

        Camera ca = null;
        Light li = null;
        GameObject pl = null;

        GameObject avatar = null;
        GameObject stepstone = null;

        private IEnumerator UploadTestAvatar()
        {
            (AsyncOperationExecutor<Context> ao, Context co) =
                AssetUploader.PrepareUploadToIPFS(Asset_iws, false);

            yield return ao.ExecuteCoroutine(co);

            AvatarCid = AssetUploader.GetUploadedCid(co);

            Assert.IsNotNull(AvatarCid);
        }

        [UnityTearDown]
        public IEnumerator Teardown0()
        {
            if (avatar != null) Object.Destroy(avatar);
            if (stepstone != null) Object.Destroy(stepstone);

            yield return null;
        }
        private GameObject CreateSteppingStone(bool right)
        {
            stepstone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stepstone.transform.localScale = Vector3.one * 0.25f;
            stepstone.transform.position = new(right ? 0.20f : -0.20f, 0);
            return stepstone;
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

            yield return ao.ExecuteCoroutine(co);

            Assert.IsNotNull(AvatarDownloader.GetLoadedAvatar(co));

            avatar = AvatarDownloader.GetLoadedAvatar(co);
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

            Assert.IsTrue(am.Feet[0].Elevation > 0.123f);
            Assert.IsTrue(am.Feet[0].Elevation < 0.124f);

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

            yield return ao.ExecuteCoroutine(co);

            avatar = AvatarDownloader.GetLoadedAvatar(co);
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

            yield return ao.ExecuteCoroutine(co);

            avatar = AvatarDownloader.GetLoadedAvatar(co);
            avatar.SetActive(true);

            IAvatarMeasures am = AvatarDownloader.GetAvatarMeasures(co);

            // Read*Joints is false, no joints transmission
            Assert.AreEqual(0, am.JointNames.Count);
            // yield return UnityPAK();
        }

        [UnityTest]
        public IEnumerator ReadRemoteJointNames()
        {
            (AsyncOperationExecutor<Context> ao, Context co) =
                AvatarDownloader.PrepareDownloadAvatar(AvatarCid, new AvatarDownloaderOptions()
                {
                    ReadFootJoints = true,
                    ReadHandJoints = true
                });

            yield return ao.ExecuteCoroutine(co);

            avatar = AvatarDownloader.GetLoadedAvatar(co);
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

            yield return ao.ExecuteCoroutine(co);

            avatar = AvatarDownloader.GetLoadedAvatar(co);
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

            yield return ao.ExecuteCoroutine(co);

            avatar = AvatarDownloader.GetLoadedAvatar(co);
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

            Assert.IsTrue(am.Feet[0].Elevation > 0.123f * transform.localScale.x);
            Assert.IsTrue(am.Feet[0].Elevation < 0.124f * transform.localScale.x);
        }

        [UnityTest]
        public IEnumerator AnimateAvatar()
        {
            (AsyncOperationExecutor<Context> ao, Context co) =
                AvatarDownloader.PrepareDownloadAvatar(AvatarCid, new AvatarDownloaderOptions()
                {
                    InstallAnimController = true
                });

            yield return ao.ExecuteCoroutine(co);

            avatar = AvatarDownloader.GetLoadedAvatar(co);
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

            yield return ao.ExecuteCoroutine(co);

            avatar = AvatarDownloader.GetLoadedAvatar(co);
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

        struct BoneTranslation
        {
            public string BoneName;
            public string HumanName;
        }
        class Table
        {
            public Dictionary<string, string> translationTable;
        }

#if false
        [UnityTest]
        [Ignore("To convert Ready Player Me's skeleton structure to JSON")]
        public IEnumerator SaveBoneTranslationTable()
        {
            yield return null;

            const string femaleAvatarResource = "AvatarAnim/RPM_FemaleAvatar";

            UnityEngine.Avatar avatar_bp =
                Resources.Load<UnityEngine.Avatar>(femaleAvatarResource);


            Table table = new() { translationTable = new() };
            foreach (HumanBone bone in avatar_bp.humanDescription.human)
                table.translationTable.Add(bone.boneName, bone.humanName);

            string json = JsonConvert.SerializeObject(table, Formatting.Indented);
            File.WriteAllText("RPMBoneTranslations.json", json);
        }
#endif
    }
}
