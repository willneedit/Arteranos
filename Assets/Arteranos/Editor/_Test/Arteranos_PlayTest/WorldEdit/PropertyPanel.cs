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
    public class PropertyPanel : WorldEditFixture
    {
        private GameObject canvas = null;
        private GameObject chooserPanel = null;
        private GameObject propertyPanel = null;
        private Transform ItemContainer = null;

        private Arteranos.WorldEdit.PropertyPanel panel = null;

        WorldObject wob = null;

        [UnitySetUp]
        public IEnumerator SetUp1()
        {
            wob = BuildSample();

            yield return wob.Instantiate(pl.transform);

            yield return ShowPanel();
        }


        [UnityTearDown]
        public IEnumerator TearDown1()
        {
            yield return null;

            Object.Destroy(canvas);
        }

        public IEnumerator ShowPanel()
        {
            GameObject canvasBlueprint = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arteranos/Editor/_Test/Canvas_Preferences_Edit.prefab");
            canvas = Object.Instantiate(canvasBlueprint);

            GameObject chooserPanelBlueprint = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arteranos/WorldEdit/UI/WorldObjectList.prefab");
            chooserPanel = Object.Instantiate(chooserPanelBlueprint, canvas.transform, false);
            ItemContainer = chooserPanel.transform.GetChild(1);

            GameObject PPBlueprint = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arteranos/WorldEdit/UI/PropertyPanel.prefab");
            PPBlueprint.SetActive(false);
            propertyPanel = Object.Instantiate(PPBlueprint, canvas.transform, false);
            PPBlueprint.SetActive(true);

            Assert.True(propertyPanel.TryGetComponent(out panel));

            yield return new WaitForEndOfFrame();

            Assert.True(GetWOChooserItem(1).btn_Property.isActiveAndEnabled);

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
        public IEnumerator T001_SwitchToPropertyPage()
        {
            GetWOChooserItem(1).Test_OnPropertyPageClicked(); // Select the Test Cube's property page
            yield return new WaitForEndOfFrame();

            Assert.False(chooserPanel.activeSelf);
            Assert.True(propertyPanel.activeSelf);

            Assert.True(panel.chk_Local.isOn);
            Assert.False(panel.chk_Global.isOn);

            Assert.AreEqual("Test Cube", panel.lbl_Heading.text);
            Assert.AreEqual("0.000", panel.txt_Pos_X.text);
            Assert.AreEqual("1.000", panel.txt_Pos_Y.text);
            Assert.AreEqual("5.000", panel.txt_Pos_Z.text);

            Assert.AreEqual("0.000", panel.txt_Rot_X.text);
            Assert.AreEqual("0.000", panel.txt_Rot_Y.text);
            Assert.AreEqual("0.000", panel.txt_Rot_Z.text);

            Assert.AreEqual("1.000", panel.txt_Scale_X.text);
            Assert.AreEqual("1.000", panel.txt_Scale_Y.text);
            Assert.AreEqual("1.000", panel.txt_Scale_Z.text);

            Assert.AreEqual("1.000", panel.txt_Col_R.text);
            Assert.AreEqual("1.000", panel.txt_Col_G.text);
            Assert.AreEqual("1.000", panel.txt_Col_B.text);
        }

        [UnityTest]
        public IEnumerator T002_Position()
        {
            GetWOChooserItem(1).WorldObject.transform.localPosition = new Vector3(1, 2, 3);

            GetWOChooserItem(1).Test_OnPropertyPageClicked(); // Select the Test Cube's property page
            yield return new WaitForEndOfFrame();

            Assert.AreEqual("1.000", panel.txt_Pos_X.text);
            Assert.AreEqual("2.000", panel.txt_Pos_Y.text);
            Assert.AreEqual("3.000", panel.txt_Pos_Z.text);
        }

        [UnityTest]
        public IEnumerator T003_Rotation()
        {
            GetWOChooserItem(1).WorldObject.transform.localRotation = Quaternion.Euler(10, 20, 0);

            GetWOChooserItem(1).Test_OnPropertyPageClicked(); // Select the Test Cube's property page
            yield return new WaitForEndOfFrame();

            Assert.AreEqual("10.000", panel.txt_Rot_X.text);
            Assert.AreEqual("20.000", panel.txt_Rot_Y.text);
            Assert.AreEqual("0.000", panel.txt_Rot_Z.text);
        }

        [UnityTest]
        public IEnumerator T004_Scale()
        {
            GetWOChooserItem(1).WorldObject.transform.localScale = new Vector3(0.1f, 0.2f, 0.3f);

            GetWOChooserItem(1).Test_OnPropertyPageClicked(); // Select the Test Cube's property page
            yield return new WaitForEndOfFrame();

            Assert.AreEqual("0.100", panel.txt_Scale_X.text);
            Assert.AreEqual("0.200", panel.txt_Scale_Y.text);
            Assert.AreEqual("0.300", panel.txt_Scale_Z.text);
        }

        [UnityTest]
        public IEnumerator T005_Color() 
        {
            if (GetWOChooserItem(1).WorldObject.TryGetComponent(out Renderer renderer))
                renderer.material.color = new Color(0.2f, 0.4f, 0.6f);

            GetWOChooserItem(1).Test_OnPropertyPageClicked(); // Select the Test Cube's property page
            yield return new WaitForEndOfFrame();

            Assert.AreEqual("0.200", panel.txt_Col_R.text);
            Assert.AreEqual("0.400", panel.txt_Col_G.text);
            Assert.AreEqual("0.600", panel.txt_Col_B.text);
        }

        [UnityTest]
        public IEnumerator T006_ReturnToChooser()
        {
            GetWOChooserItem(1).Test_OnPropertyPageClicked(); // Select the Test Cube's property page
            yield return new WaitForEndOfFrame();

            panel.Test_OnReturnToChooserClicked();

            yield return new WaitForEndOfFrame();

            Assert.True(chooserPanel.activeSelf);
            Assert.False(propertyPanel.activeSelf);

        }
    }
}