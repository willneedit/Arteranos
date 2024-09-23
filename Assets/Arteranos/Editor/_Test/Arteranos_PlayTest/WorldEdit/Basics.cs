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
using Arteranos.WorldEdit.Components;

namespace Arteranos.PlayTest.WorldEdit
{
    public class Basics : WorldEditFixture
    {
        [UnityTest]
        public IEnumerator T000_DefaultTestObject()
        {
            WorldObject wob = BuildSample();

            yield return ShowObject(wob);
        }

        [UnityTest]
        public IEnumerator T001_Position()
        {
            WorldObject wob = BuildSample();

            wob.GetWComponent<WOCTransform>().position += new Vector3(2, 0, 0);

            yield return ShowObject(wob);
        }

        [UnityTest]
        public IEnumerator T002_Rotation()
        {
            WorldObject wob = BuildSample();

            WOCTransform wOCTransform = wob.GetWComponent<WOCTransform>();
            wOCTransform.rotation += new Vector3(0, 45, 0);

            yield return ShowObject(wob);
        }

        [UnityTest]
        public IEnumerator T003_Scale()
        {
            WorldObject wob = BuildSample();

            wob.GetWComponent<WOCTransform>().scale = new Vector3(0.5f, 0.5f, 0.5f);

            yield return ShowObject(wob);
        }

        [UnityTest]
        public IEnumerator T004_Color()
        {
            WorldObject wob = BuildSample();

            wob.GetWComponent<WOCColor>().color = Color.red;

            yield return ShowObject(wob);
        }

        [UnityTest]
        public IEnumerator T005_ReRoot()
        {
            WorldObject wob = BuildSample();

            WorldObject newroot = new() { components = new() { new WOCTransform() } };
            newroot.GetWComponent<WOCTransform>().position = new Vector3(-2, 0, 0);
            newroot.GetWComponent<WOCTransform>().rotation = new Vector3(0, 0, -45);
            newroot.children.Add(wob);

            yield return ShowObject(newroot);
        }


    }
}