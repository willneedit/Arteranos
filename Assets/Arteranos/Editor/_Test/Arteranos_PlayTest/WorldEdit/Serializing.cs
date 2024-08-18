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

            Transform tc = pl.transform.GetChild(0);

            Assert.AreEqual("Test Cube", tc.name);

            WorldObjectComponent woc = tc.GetComponent<WorldObjectComponent>();

            Assert.IsNotNull(woc);
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
    }
}