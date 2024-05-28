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

        Camera ca = null;
        Light li = null;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            ca = new GameObject("Camera").AddComponent<Camera>();
            ca.transform.position = new(0, 1.75f, 0.2f);

            li = new GameObject("Light").AddComponent<Light>();
            li.transform.SetPositionAndRotation(new(0, 3, 0), Quaternion.Euler(50, -30, 0));
            li.type = LightType.Directional;
            li.color = Color.white;

            GameObject bpl = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arteranos/Editor/_Test/Plane.prefab");
            pl = Object.Instantiate(bpl);

            yield return new WaitForEndOfFrame();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(pl);
            Object.Destroy(ca.gameObject);
            Object.Destroy(li.gameObject);

            yield return null;
        }

        // Must match the sample constructed with the following routine

        public const string sampleWOB = 
            "CgaagCACCAMSCVRlc3QgQ3ViZRoKFQAAgD8dAACgQCIFJQAAgD8qDw0AAIA/F" +
            "QAAgD8dAACAPzIUDQAAgD8VAACAPx0AAIA/JQAAgD9ClQEKBJqAIAASC1Rlc3" +
            "QgU3BoZXJlGgUVAADAPyIFJQAAgD8qDw0AAIA/FQAAgD8dAACAPzIUDQAAgD8" +
            "VAACAPx0AAIA/JQAAgD9CSwoGmoAgAggBEgxUZXN0IENhcHN1bGUaBRUAAMA/" +
            "IgUlAACAPyoPDQAAgD8VAACAPx0AAIA/MhQNAACAPxUAAIA/HQAAgD8lAACAP" +
            "0JOCgaagCACCAMSD1Rlc3QgQ3ViZSBSaWdodBoFDQAAwD8iBSUAAIA/Kg8NAA" +
            "CAPxUAAIA/HQAAgD8yFA0AAIA/FQAAgD8dAACAPyUAAIA/";

        public WorldObject BuildSample()
        {
            WorldObject cube = new(PrimitiveType.Cube)
            {
                position = new Vector3(0, 1, 5),
                name = "Test Cube"
            };

            WorldObject cube2 = new(PrimitiveType.Cube)
            {
                position = new Vector3(1.5f, 0, 0),
                name = "Test Cube Right"
            };

            WorldObject sphere = new(PrimitiveType.Sphere)
            {
                position = new Vector3(0, 1.5f, 0),
                name = "Test Sphere"
            };

            WorldObject capsule = new(PrimitiveType.Capsule)
            {
                position = new Vector3(0, 1.5f, 0),
                name = "Test Capsule"
            };

            sphere.children.Add(capsule);
            cube.children.Add(sphere);
            cube.children.Add(cube2);

            return cube;
        }

        public IEnumerator ShowObject(WorldObject wob)
        {
            yield return wob.Instantiate(pl.transform);

            yield return new WaitForSeconds(5);

        }
    }
}
