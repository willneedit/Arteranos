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

using Vector3 = Arteranos.WorldEdit.WOVector3;
using Quaternion = Arteranos.WorldEdit.WOQuaternion;
using Color = Arteranos.WorldEdit.WOColor;
using Arteranos.WorldEdit;

namespace Arteranos.PlayTest.WorldEdit
{
    public class WorldEditFixture
    {
        Camera ca = null;
        Light li = null;
        GameObject pl = null;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            ca = new GameObject("Camera").AddComponent<Camera>();
            ca.transform.position = new(0, 1.75f, 0.2f);

            li = new GameObject("Light").AddComponent<Light>();
            li.transform.SetPositionAndRotation(new(0, 3, 0), UnityEngine.Quaternion.Euler(50, -30, 0));
            li.type = LightType.Directional;
            li.color = UnityEngine.Color.white;

            GameObject bpl = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arteranos/Editor/_Test/Plane.prefab");
            pl = Object.Instantiate(bpl);

            yield return new WaitForEndOfFrame();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return new WaitForSeconds(5);

            Object.Destroy(pl);
            Object.Destroy(ca.gameObject);
            Object.Destroy(li.gameObject);
        }

        public WorldObject BuildPrimitives()
        {
            WorldObject cube = new(PrimitiveType.Cube);
            cube.position = new UnityEngine.Vector3(0, 1, 5);
            cube.asset.name = "Test Cube";

            WorldObject cube2 = new(PrimitiveType.Cube);
            cube2.position = new UnityEngine.Vector3(1.5f, 0, 0);
            cube2.asset.name = "Test Cube Right";

            WorldObject sphere = new(PrimitiveType.Sphere);
            sphere.position = new UnityEngine.Vector3(0, 1.5f, 0);
            sphere.asset.name = "Test Sphere";

            WorldObject capsule = new(PrimitiveType.Capsule);
            capsule.position = new UnityEngine.Vector3(0, 1.5f, 0);
            capsule.asset.name = "Test Capsule";

            sphere.children.Add(capsule);
            cube.children.Add(sphere);
            cube.children.Add(cube2);

            return cube;
        }

        public IEnumerator ShowObject(WorldObject wob, Transform parent)
        {
            GameObject gob = null;

            if (wob.asset is WOPrimitive wopr)
                gob = GameObject.CreatePrimitive(wopr.primitive);

            if (gob != null)
            {
                if (wob.asset != null)
                    gob.name = wob.asset.name;

                Transform t = gob.transform;
                t.SetParent(parent);
                t.localPosition = wob.position;
                t.localRotation = wob.rotation;
                t.localScale = wob.scale;

                foreach(WorldObject child in wob.children)
                    yield return ShowObject(child, t);
            }

            yield return new WaitForEndOfFrame();
        }

        [UnityTest]
        public IEnumerator DefaultTestObject()
        {
            WorldObject wob = BuildPrimitives();

            yield return ShowObject(wob, pl.transform);
        }
    }
}
