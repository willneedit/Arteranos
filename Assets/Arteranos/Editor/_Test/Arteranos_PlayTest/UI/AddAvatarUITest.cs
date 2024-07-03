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
        private const string Asset_iws = "file:///Assets/Arteranos/Editor/_Test/6394c1e69ef842b3a5112221.glb";

        IPFSServiceImpl service = null;
        AddAvatarUI aaui = null;

        [UnitySetUp]
        public IEnumerator Setup0()
        {
            TestFixtures.SceneFixture(ref ca, ref li, ref pl);

            TestFixtures.IPFSServiceFixture(ref service);

            yield return TestFixtures.StartIPFSAndWait(service);
        }

        [UnityTearDown]
        public IEnumerator TearDown0()
        {
            if (aaui != null) Object.Destroy(aaui.gameObject);
            yield return null;
        }
        Camera ca = null;
        Light li = null;
        GameObject pl = null;

        public IEnumerator UnityPAK()
        {
            GameObject go = new("Delete me to continue!");

            while (go != null) { yield return new WaitForSeconds(1f); }
        }

        [UnityTest]
        public IEnumerator Open_Close()
        {
            yield return null;

            aaui = AddAvatarUI.New();
            yield return new WaitForSeconds(5);
            Object.Destroy(aaui.gameObject);
        }

        [UnityTest]
        public IEnumerator LoadAvatar()
        {
            aaui = AddAvatarUI.New();
            yield return new WaitForSeconds(1);

            aaui.Test_AvatarURL = Asset_iws;

            yield return new WaitForSeconds(1);

            aaui.Test_OnAddAvatarClicked();

            // yield return UnityPAK();
        }

        [UnityTest]
        public IEnumerator LoadAvatarUndecided()
        {
            aaui = AddAvatarUI.New();
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

            aaui = AddAvatarUI.New();
            yield return null;

            aaui.Test_AvatarURL = "C:\\Does.Not.exist";

            yield return new WaitForSeconds(1);

            aaui.Test_OnAddAvatarClicked();

            yield return new WaitForSeconds(1);

        }
    }
}
