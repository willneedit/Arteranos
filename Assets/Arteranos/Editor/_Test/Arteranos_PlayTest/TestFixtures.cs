using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using Ipfs.Http;
using Arteranos.Services;
using System;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using UnityEditor;

namespace Arteranos.PlayTest
{
    public class TestFixtures
    {

        public static GameObject SetupStartupManagerMock()
        {
            GameObject bp = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arteranos/_Test/Arteranos_PlayTest/SceneEssentialsMock.prefab");
            return UnityEngine.Object.Instantiate(bp);
        }

        public static IEnumerator WaitForCondition(int timeoutSeconds, Func<bool> condition, string message)
        {
            DateTime expiry = DateTime.Now + TimeSpan.FromSeconds(timeoutSeconds);
            while(!condition())
            {
                if (expiry < DateTime.Now) Assert.Fail(message);
                yield return new WaitForEndOfFrame();
            }
        }

        public static async Task WaitForConditionAsync(int timeoutSeconds, Func<bool> condition, string message)
        {
            DateTime expiry = DateTime.Now + TimeSpan.FromSeconds(timeoutSeconds);
            while (!condition())
            {
                if (expiry < DateTime.Now)
                {
                    Debug.LogError(message);
                    Assert.Fail(message);
                }
                await Task.Delay(8);
            }
        }

        public static void SceneFixture(ref Camera ca, ref Light li, ref GameObject pl)
        {
            ca = GameObject.FindObjectOfType<Camera>();
            if (ca == null)
            {
                ca = new GameObject("Camera").AddComponent<Camera>();
                ca.transform.position = new(0, 1.75f, 0.2f);
            }

            li = GameObject.FindObjectOfType<Light>();
            if (li == null)
            {
                li = new GameObject("Light").AddComponent<Light>();
                li.transform.SetPositionAndRotation(new(0, 3, 0), Quaternion.Euler(50, -30, 0));
                li.type = LightType.Directional;
                li.color = Color.white;
            }

            pl = GameObject.FindGameObjectWithTag("WorldObjectsRoot");
            if (pl == null)
            {
                pl = GameObject.CreatePrimitive(PrimitiveType.Plane);
                pl.AddComponent<Arteranos.WorldEdit.WorldEditorData>();
                pl.tag = "WorldObjectsRoot";
            }
        }

        public static void IPFSServiceFixture(ref IPFSServiceImpl service)
        {
            service = GameObject.FindObjectOfType<IPFSServiceImpl>(true);
            if(service == null)
            {
                GameObject sce_bp = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arteranos/Editor/_Test/_SceneEssentials Variant.prefab");
                sce_bp.SetActive(false);
                GameObject sce = GameObject.Instantiate(sce_bp);
                if (!sce.TryGetComponent(out service))
                    Assert.Fail("No IPFS service implementation");

                // Bare essentials, to not to interfere with the tests
                service.EnablePublishServerData = false;
                service.EnableServerDiscovery = false;
                service.EnableUploadDefaultAvatars = false;

                // Leave it disabled.
            }
        }

        public static IEnumerator StartIPFSAndWait(IPFSServiceImpl service)
        {
            service.enabled = true;
            service.gameObject.SetActive(true);

            yield return new WaitUntil(() => service.Ipfs_ != null);
        }
    }
}
