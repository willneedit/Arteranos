/*
 *Copyright(c) 2024, willneedit
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
    public class Serializing : WorldEditFixture
    {
        [UnityTest]
        public IEnumerator Serialize()
        {
            WorldObject wob = BuildSample();

            using MemoryStream ms = new();
            wob.Serialize(ms);
            string s = Convert.ToBase64String(ms.ToArray());

            Debug.Log(s);

            Assert.AreEqual(sampleWOB, s);

            yield return null;
        }

        [UnityTest]
        public IEnumerator Deserialize()
        {
            using MemoryStream ms = new(Convert.FromBase64String(sampleWOB));
            WorldObject wob = WorldObject.Deserialize(ms);

            yield return ShowObject(wob);
        }

        [UnityTest]
        public IEnumerator ReRoot()
        {
            WorldObject wob = BuildSample();

            WorldObject newroot = new()
            {
                position = new Vector3(-2, 0, 0),
                rotation = Quaternion.Euler(0, 0, -45),
            };
            newroot.children.Add(wob);

            yield return ShowObject(wob);
        }

        [UnityTest]
        public IEnumerator Disassemble()
        {
            WorldObject wob = BuildSample();

            using MemoryStream ms = new();
            wob.Serialize(ms);
            string s = Convert.ToBase64String(ms.ToArray());

            GameObject result = null;
            yield return wob.Instantiate(pl.transform, _go => result = _go);

            WorldObject wob2 = result.MakeWorldObject();
            using MemoryStream ms2 = new();
            wob2.Serialize(ms2);
            string s2 = Convert.ToBase64String(ms2.ToArray());

            Assert.AreEqual(s, s2);
        }
    }
}