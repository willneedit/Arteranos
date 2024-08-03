/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using Arteranos.WorldEdit;
using Object = UnityEngine.Object;
using Arteranos.Core;

namespace Arteranos.PlayTest.WorldEdit
{
    public class WorldEditFixture
    {
        public GameObject pl = null;

        Camera ca = null;
        Light li = null;

        [UnitySetUp]
        public IEnumerator SetUp0()
        {
            TestFixtures.SceneFixture(ref ca, ref li, ref pl);

            Assert.IsNotNull(G.WorldEditorData);

            TestFixtures.WithBlueprintdFixture();

            Assert.IsNotNull(BP.I);

            yield return new WaitForEndOfFrame();
        }

        [UnityTearDown]
        public IEnumerator TearDown0()
        {
            Object.Destroy(pl);
            //Object.Destroy(ca.gameObject);
            //Object.Destroy(li.gameObject);

            yield return null;
        }

        // Must match the sample constructed with the following routine
        public static readonly string sampleWOB =
            "CgaagCACCAMSCVRlc3QgQ3ViZSABOiiKgCAkCgoVAACAPx0AAKBAEgUlAACAP" +
            "xoPDQAAgD8VAACAPx0AAIA/OhqSgCAWChQNAACAPxUAAIA/HQAAgD8lAACAP0" +
            "KtAQoEmoAgABILVGVzdCBTcGhlcmU6I4qAIB8KBRUAAMA/EgUlAACAPxoPDQA" +
            "AgD8VAACAPx0AAIA/OhqSgCAWChQNAACAPxUAAIA/HQAAgD8lAACAP0JXCgaa" +
            "gCACCAESDFRlc3QgQ2Fwc3VsZTojioAgHwoFFQAAwD8SBSUAAIA/Gg8NAACAP" +
            "xUAAIA/HQAAgD86GpKAIBYKFA0AAIA/FQAAgD8dAACAPyUAAIA/QloKBpqAIA" +
            "IIAxIPVGVzdCBDdWJlIFJpZ2h0OiOKgCAfCgUNAADAPxIFJQAAgD8aDw0AAIA" +
            "/FQAAgD8dAACAPzoakoAgFgoUDQAAgD8VAACAPx0AAIA/JQAAgD8=";

        public static WorldObject BuildSample()
        {
            WorldObject cube = new(PrimitiveType.Cube)
            {
                name = "Test Cube",
                collidable = true,
            };
            cube.GetWComponent<WOCTransform>().position = new Vector3(0, 1, 5);

            WorldObject cube2 = new(PrimitiveType.Cube)
            {
                name = "Test Cube Right"
            };
            cube2.GetWComponent<WOCTransform>().position = new Vector3(1.5f, 0, 0);

            WorldObject sphere = new(PrimitiveType.Sphere)
            {
                name = "Test Sphere"
            };
            sphere.GetWComponent<WOCTransform>().position = new Vector3(0, 1.5f, 0);

            WorldObject capsule = new(PrimitiveType.Capsule)
            {
                name = "Test Capsule"
            };
            capsule.GetWComponent<WOCTransform>().position = new Vector3(0, 1.5f, 0);

            sphere.children.Add(capsule);
            cube.children.Add(sphere);
            cube.children.Add(cube2);

            return cube;
        }

        public static void ClearGUIDs(WorldObject wob)
        {
            wob.id = new();
            foreach(var child in wob.children)
                ClearGUIDs(child);
        }

        public IEnumerator ShowObject(WorldObject wob)
        {
            yield return wob.Instantiate(pl.transform);

            yield return new WaitForSeconds(5);

        }
    }
}
