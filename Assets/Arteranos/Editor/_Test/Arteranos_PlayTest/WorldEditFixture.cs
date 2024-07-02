/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

using Arteranos.WorldEdit;
using System.IO;
using System;
using Object = UnityEngine.Object;

namespace Arteranos.PlayTest.WorldEdit
{
    public class WorldEditFixture
    {
        public GameObject pl = null;
        public WorldEditorData editorData = null;

        Camera ca = null;
        Light li = null;

        [UnitySetUp]
        public IEnumerator SetUp0()
        {
            TestFixtures.SceneFixture(ref ca, ref li, ref pl);

            if (!pl.TryGetComponent(out editorData))
                Assert.Fail("No EditorData component");

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
        public const string sampleWOB =
            "CgaagCACCAMSCVRlc3QgQ3ViZTooioAgJAoKFQAAgD8dAACgQBIFJQAAgD8aD" +
            "w0AAIA/FQAAgD8dAACAPzoakoAgFgoUDQAAgD8VAACAPx0AAIA/JQAAgD9CrQ" +
            "EKBJqAIAASC1Rlc3QgU3BoZXJlOiOKgCAfCgUVAADAPxIFJQAAgD8aDw0AAIA" +
            "/FQAAgD8dAACAPzoakoAgFgoUDQAAgD8VAACAPx0AAIA/JQAAgD9CVwoGmoAg" +
            "AggBEgxUZXN0IENhcHN1bGU6I4qAIB8KBRUAAMA/EgUlAACAPxoPDQAAgD8VA" +
            "ACAPx0AAIA/OhqSgCAWChQNAACAPxUAAIA/HQAAgD8lAACAP0JaCgaagCACCA" +
            "MSD1Rlc3QgQ3ViZSBSaWdodDojioAgHwoFDQAAwD8SBSUAAIA/Gg8NAACAPxU" +
            "AAIA/HQAAgD86GpKAIBYKFA0AAIA/FQAAgD8dAACAPyUAAIA/";

        public WorldObject BuildSample()
        {
            WorldObject cube = new(PrimitiveType.Cube)
            {
                name = "Test Cube"
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

        public void ClearGUIDs(WorldObject wob)
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
