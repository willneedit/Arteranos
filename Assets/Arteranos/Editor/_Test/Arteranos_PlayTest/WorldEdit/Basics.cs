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
    public class Basics : WorldEditFixture
    {
        [UnityTest]
        public IEnumerator DefaultTestObject()
        {
            WorldObject wob = BuildSample();

            yield return ShowObject(wob);
        }

        [UnityTest]
        public IEnumerator Position()
        {
            WorldObject wob = BuildSample();

            wob.position += new Vector3(2, 0, 0);

            yield return ShowObject(wob);
        }

        [UnityTest]
        public IEnumerator Rotation()
        {
            WorldObject wob = BuildSample();

            wob.rotation *= Quaternion.Euler(0, 45, 0);

            yield return ShowObject(wob);
        }

        [UnityTest]
        public IEnumerator Scale()
        {
            WorldObject wob = BuildSample();

            wob.scale = new Vector3(0.5f, 0.5f, 0.5f);

            yield return ShowObject(wob);
        }

        [UnityTest]
        public IEnumerator Color()
        {
            WorldObject wob = BuildSample();

            wob.color = UnityEngine.Color.red;

            yield return ShowObject(wob);
        }


    }
}