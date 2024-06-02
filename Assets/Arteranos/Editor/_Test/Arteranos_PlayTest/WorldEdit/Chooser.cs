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
    public class Chooser : WorldEditFixture
    {
        private GameObject panel = null;
        private Transform ItemContainer = null;

        public IEnumerator ShowPanel()
        {
            GameObject canvasBlueprint = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arteranos/Editor/_Test/Canvas_Preferences_Edit.prefab");
            GameObject canvas = Object.Instantiate(canvasBlueprint);

            GameObject panelBlueprint = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arteranos/WorldEdit/UI/WorldObjectList.prefab");
            panel = Object.Instantiate(panelBlueprint, canvas.transform, false);
            ItemContainer = panel.transform.GetChild(1);

            yield return null;
        }

        public WorldObjectListItem GetWOChooserItem(int index)
        {
            Transform panels = ItemContainer;

            GameObject go = index < panels.childCount
                ? panels.GetChild(index).gameObject
                : null;

            return go != null ? go.GetComponent<WorldObjectListItem>() : null;
        }

        [UnityTest]
        public IEnumerator T001_Show()
        {
            WorldObject wob = BuildSample();

            yield return wob.Instantiate(pl.transform);

            yield return ShowPanel();

            Assert.AreEqual(2, ItemContainer.childCount);
            Assert.AreEqual("(Root)", GetWOChooserItem(0).txt_Name.text);
            Assert.AreEqual("Test Cube", GetWOChooserItem(1).txt_Name.text);
            Assert.False(GetWOChooserItem(0).btn_ToParent.isActiveAndEnabled); // Item 0 is the root
            Assert.False(GetWOChooserItem(1).btn_ToParent.isActiveAndEnabled); // Item 1 and more

            Assert.False(GetWOChooserItem(0).btn_ToChild.isActiveAndEnabled);  // Item 0 is just the heading
            Assert.True(GetWOChooserItem(1).btn_ToChild.isActiveAndEnabled);   // Item 1 does have children

            yield return new WaitForEndOfFrame();
        }

        [UnityTest]
        public IEnumerator T002_Descend() 
        {
            WorldObject wob = BuildSample();

            yield return wob.Instantiate(pl.transform);

            yield return ShowPanel();

            WorldObjectListItem wo0 = GetWOChooserItem(0);
            WorldObjectListItem wo1 = GetWOChooserItem(1);
        }

    }
}