/*
 *Copyright(c) 2024, willneedit
 *
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;


using Arteranos.WorldEdit;
using System.IO;
using System;

namespace Arteranos.PlayTest.WorldEdit
{
    public class Serializing : WorldEditFixture
    {
        [UnityTest]
        public IEnumerator T001_Serialize()
        {
            WorldObject wob = BuildSample();
            ClearGUIDs(wob);

            using MemoryStream ms = new();
            wob.Serialize(ms);
            string s = Convert.ToBase64String(ms.ToArray());

            Debug.Log(s);

            Assert.AreEqual(sampleWOB, s);

            yield return null;
        }

        [UnityTest]
        public IEnumerator T002_Deserialize()
        {
            using MemoryStream ms = new(Convert.FromBase64String(sampleWOB));
            WorldObject wob = WorldObject.Deserialize(ms);

            yield return ShowObject(wob);
        }

        [UnityTest]
        public IEnumerator T003_Disassemble()
        {
            WorldObject wob = BuildSample();

            using MemoryStream ms = new();
            wob.Serialize(ms);
            string s = Convert.ToBase64String(ms.ToArray());

            GameObject result = null;
            yield return wob.Instantiate(pl.transform, _go => result = _go);

            WorldObject wob2 = result.MakeWorldObject();

            //yield return wob2.Instantiate(pl.transform);
            //yield return new WaitForSeconds(180);

            using MemoryStream ms2 = new();
            wob2.Serialize(ms2);
            string s2 = Convert.ToBase64String(ms2.ToArray());

            Assert.AreEqual(s, s2);
        }

        [UnityTest]
        public IEnumerator T004_Patch()
        {
            WorldObject wob = BuildSample();

            // First, build and instantiate the sample.
            GameObject result = null;
            yield return wob.Instantiate(pl.transform, _go => result = _go);

            // Pick the sphere...
            WorldObject sphere = wob.children[0];
            Assert.IsNotNull(sphere);

            // First and second components are the transform or the color, respectively.
            WOCTransform t = sphere.GetWComponent<WOCTransform>();
            WOCColor c = sphere.GetWComponent<WOCColor>();
            Assert.IsNotNull(t);
            Assert.IsNotNull(c);

            t.rotation = Quaternion.Euler(0, 0, 45);
            c.color = Color.red;

            yield return new WaitForSeconds(2);

            sphere.Patch();

            yield return new WaitForSeconds(2);

            Assert.AreEqual(2, sphere.components.Count);
        }
    }
}