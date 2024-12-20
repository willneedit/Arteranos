using System.Collections;
using NUnit.Framework;
using UnityEngine;

using Arteranos.Services;
using System;
using System.Threading.Tasks;
using UnityEditor;
using Arteranos.Core;

namespace Arteranos.PlayTest
{
    public class TestFixtures
    {

        public static void SceneFixture(ref Camera ca, ref Light li, ref GameObject pl)
        {
            ca = UnityEngine.Object.FindObjectOfType<Camera>();
            if (ca == null) ca = new GameObject("Camera").AddComponent<Camera>();

            ca.transform.position = new(0, 1.75f, 0.2f);

            li = UnityEngine.Object.FindObjectOfType<Light>();
            if (li == null) li = new GameObject("Light").AddComponent<Light>();

            li.transform.SetPositionAndRotation(new(0, 3, 0), Quaternion.Euler(50, -30, 0));
            li.type = LightType.Directional;
            li.color = Color.white;

            pl = GameObject.FindGameObjectWithTag("WorldObjectsRoot");
            if (pl == null)
            {
                pl = GameObject.CreatePrimitive(PrimitiveType.Plane);
                pl.AddComponent<Arteranos.WorldEdit.WorldEditorData>();
                pl.tag = "WorldObjectsRoot";
            }
        }

        public static void IPFSServiceFixture(ref IPFSService service)
        {
            service = UnityEngine.Object.FindObjectOfType<IPFSService>(true);
            if(service == null)
            {
                GameObject sce_bp = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arteranos/Editor/_Test/_SceneEssentials Variant.prefab");
                sce_bp.SetActive(false);
                GameObject sce = UnityEngine.Object.Instantiate(sce_bp);
                if (!sce.TryGetComponent(out service))
                    Assert.Fail("No IPFS service implementation");

                // Bare essentials, to not to interfere with the tests
                service.EnableUploadDefaultAvatars = false;

                // Leave it disabled.
            }
        }

        public static void WithBlueprintdFixture()
        {
            if(BP.I == null)
            {
                Blueprints blueprints = AssetDatabase.LoadAssetAtPath<Blueprints>("Assets/Arteranos/Modules/Core/Settings/Blueprints.asset");

                BP.I = blueprints;
            }
        }

        public static IEnumerator StartIPFSAndWait(IPFSService service)
        {
            service.enabled = true;
            service.gameObject.SetActive(true);

            yield return new WaitUntil(() => service.Ipfs != null);
        }
    }
}
