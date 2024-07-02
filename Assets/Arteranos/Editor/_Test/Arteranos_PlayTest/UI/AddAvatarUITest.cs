using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using Arteranos.Services;
using System.IO;
using System.Threading.Tasks;
using Arteranos.Core;
using Ipfs;
using Arteranos.Web;
using Arteranos.Core.Operations;
using Arteranos.UI;
using UnityEditor;
using Ipfs.Http;

namespace Arteranos.PlayTest.UI
{
    public class AddAvatarUITest
    {
        private const string Asset_iws = "file:///Assets/Arteranos/_Test/Iwontsay.glb";


        IPFSServiceImpl srv = null;
        IpfsClientEx ipfs = null;

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

            yield return TestFixtures.WaitForCondition(5, () => srv?.Ipfs_ != null, "IPFS server timeout");

            ipfs = srv.Ipfs_;

            //self = Task.Run(async () => await ipfs.LocalPeer).Result;
        }

        Camera ca = null;
        Light li = null;
        GameObject pl = null;


        [UnityTearDown]
        public IEnumerator TeardownIPFS()
        {
            if (AvatarCid != null)
            {
                ipfs.Pin.RemoveAsync(AvatarCid).Wait();
                WorldInfo.DBDelete(AvatarCid);
            }

            srv = null;
            ipfs = null;
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

        [UnityTest]
        public IEnumerator LoadAvatarUndecided()
        {
            AddAvatarUI aaui = AddAvatarUI.New();
            yield return new WaitForSeconds(1);

            aaui.Test_AvatarURL = Asset_iws;

            yield return new WaitForSeconds(1);

            aaui.Test_OnAddAvatarClicked();

            yield return new WaitForSeconds(1);

            aaui.Test_AvatarURL = Asset_iws[0..^2]; // User changes URL....

            yield return new WaitForSeconds(2);

            aaui.Test_AvatarURL = Asset_iws; // ... but changes his mind to the loaded avatar again

            yield return new WaitForSeconds(2);
        }

        [UnityTest]
        public IEnumerator LoadWithFailingURLs()
        {
            LogAssert.Expect(LogType.Exception, "FileNotFoundException: Could not find file 'C:\\Does.Not.exist'.");

            AddAvatarUI aaui = AddAvatarUI.New();
            yield return null;

            aaui.Test_AvatarURL = "C:\\Does.Not.exist";

            yield return new WaitForSeconds(1);

            aaui.Test_OnAddAvatarClicked();

            yield return new WaitForSeconds(1);

        }
    }
}
