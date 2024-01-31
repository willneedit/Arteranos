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
using Arteranos.UI;
using UnityEditor;

namespace Arteranos.PlayTest.UI
{
    public class AddAvatarUITest
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
        }

        Camera ca = null;
        Light li = null;
        GameObject pl = null;


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

        private void SetupScene()
        {
            ca = new GameObject("Camera").AddComponent<Camera>();
            ca.transform.position = new(0, 1.75f, 0.2f);

            li = new GameObject("Light").AddComponent<Light>();
            li.transform.SetPositionAndRotation(new(0, 3, 0), Quaternion.Euler(50, -30, 0));
            li.type = LightType.Directional;
            li.color = Color.white;

            GameObject bpl = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arteranos/_Test/Plane.prefab");
            pl = Object.Instantiate(bpl);
        }



        public IEnumerator UnityPAK()
        {
            GameObject go = new("Delete me to continue!");

            while (go != null) { yield return new WaitForSeconds(1f); }
        }

        [UnityTest]
        public IEnumerator Open_Close()
        {
            yield return null;

            AddAvatarUI aaui = AddAvatarUI.New();
            yield return new WaitForSeconds(5);
            Object.Destroy(aaui.gameObject);
        }

        [UnityTest]
        public IEnumerator LoadAvatar()
        {
            AddAvatarUI aaui = AddAvatarUI.New();
            yield return new WaitForSeconds(1);

            aaui.Test_AvatarURL = Asset_iws;

            yield return new WaitForSeconds(1);

            aaui.Test_OnAddAvatarClicked();

            yield return UnityPAK();
        }
    }
}
