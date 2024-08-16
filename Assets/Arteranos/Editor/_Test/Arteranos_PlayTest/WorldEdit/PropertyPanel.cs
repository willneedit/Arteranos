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
#if false
    public class PropertyPanel : WorldEditFixture
    {
        private GameObject canvas = null;
        private GameObject editorUI = null;
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

            GameObject editorUIBlueprint = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arteranos/Modules/WorldEdit/UI/WorldEditorUI.prefab");
            editorUI = Object.Instantiate(editorUIBlueprint, canvas.transform, false);

            WorldObjectList list = editorUI.GetComponent<WorldEditorUI>().WorldObjectList;

            ItemContainer = list.transform.GetChild(1);

            Assert.IsNotNull(ItemContainer);

            chooserPanel = editorUI.GetComponent<WorldEditorUI>().WorldObjectList.gameObject;
            propertyPanel = editorUI.GetComponent<WorldEditorUI>().PropertyPanel.gameObject;
            panel = editorUI.GetComponent<WorldEditorUI>().PropertyPanel;

            Assert.IsNotNull(chooserPanel);
            Assert.IsNotNull(propertyPanel);
            Assert.IsNotNull(panel);
            Assert.AreEqual("ObjectGallery", ItemContainer.name);

            yield return new WaitForEndOfFrame();
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
            Assert.AreEqual("0.00", panel.txt_Pos_X.text);
            Assert.AreEqual("1.00", panel.txt_Pos_Y.text);
            Assert.AreEqual("5.00", panel.txt_Pos_Z.text);

            Assert.AreEqual("0.00", panel.txt_Rot_X.text);
            Assert.AreEqual("0.00", panel.txt_Rot_Y.text);
            Assert.AreEqual("0.00", panel.txt_Rot_Z.text);

            Assert.AreEqual("1.00", panel.txt_Scale_X.text);
            Assert.AreEqual("1.00", panel.txt_Scale_Y.text);
            Assert.AreEqual("1.00", panel.txt_Scale_Z.text);

            Assert.AreEqual("1.00", panel.txt_Col_R.text);
            Assert.AreEqual("1.00", panel.txt_Col_G.text);
            Assert.AreEqual("1.00", panel.txt_Col_B.text);
        }

        [UnityTest]
        public IEnumerator T002_Position()
        {
            GetWOChooserItem(1).WorldObject.transform.localPosition = new Vector3(1, 2, 3);

            GetWOChooserItem(1).Test_OnPropertyPageClicked(); // Select the Test Cube's property page
            yield return new WaitForEndOfFrame();

            Assert.AreEqual("1.00", panel.txt_Pos_X.text);
            Assert.AreEqual("2.00", panel.txt_Pos_Y.text);
            Assert.AreEqual("3.00", panel.txt_Pos_Z.text);
        }

        [UnityTest]
        public IEnumerator T003_Rotation()
        {
            GetWOChooserItem(1).WorldObject.transform.localRotation = Quaternion.Euler(10, 20, 0);

            GetWOChooserItem(1).Test_OnPropertyPageClicked(); // Select the Test Cube's property page
            yield return new WaitForEndOfFrame();

            Assert.AreEqual("10.00", panel.txt_Rot_X.text);
            Assert.AreEqual("20.00", panel.txt_Rot_Y.text);
            Assert.AreEqual("0.00", panel.txt_Rot_Z.text);
        }

        [UnityTest]
        public IEnumerator T004_Scale()
        {
            GetWOChooserItem(1).WorldObject.transform.localScale = new Vector3(0.1f, 0.2f, 0.3f);

            GetWOChooserItem(1).Test_OnPropertyPageClicked(); // Select the Test Cube's property page
            yield return new WaitForEndOfFrame();

            Assert.AreEqual("0.10", panel.txt_Scale_X.text);
            Assert.AreEqual("0.20", panel.txt_Scale_Y.text);
            Assert.AreEqual("0.30", panel.txt_Scale_Z.text);
        }

        [UnityTest]
        public IEnumerator T005_Color() 
        {
            if (GetWOChooserItem(1).WorldObject.TryGetComponent(out Renderer renderer))
                renderer.material.color = new Color(0.2f, 0.4f, 0.6f);

            GetWOChooserItem(1).Test_OnPropertyPageClicked(); // Select the Test Cube's property page
            yield return new WaitForEndOfFrame();

            Assert.AreEqual("0.20", panel.txt_Col_R.text);
            Assert.AreEqual("0.40", panel.txt_Col_G.text);
            Assert.AreEqual("0.60", panel.txt_Col_B.text);
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

        [UnityTest]
        public IEnumerator T007_MovingByObject()
        {
            WorldObjectListItem woli = GetWOChooserItem(1);
            Transform t = woli.WorldObject.transform;

            woli.Test_OnPropertyPageClicked(); // Select the Test Cube's property page
            yield return new WaitForEndOfFrame();

            // And now, we're moving the object.
            t.localPosition = new Vector3(2, 3, 4);
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            // Only local; not propagated and not set in the WorldObject.
            Assert.AreEqual("0.00", panel.txt_Pos_X.text);
            Assert.AreEqual("1.00", panel.txt_Pos_Y.text);
            Assert.AreEqual("5.00", panel.txt_Pos_Z.text);
        }

        [UnityTest]
        public IEnumerator T008_MovingByPanel()
        {
            WorldObjectListItem woli = GetWOChooserItem(1);
            Transform t = woli.WorldObject.transform;

            woli.Test_OnPropertyPageClicked(); // Select the Test Cube's property page
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            // Moving the object as you type
            panel.txt_Pos_X.text = "1.5";

            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            Assert.AreEqual(1.5f, t.localPosition.x);
            Assert.AreEqual(1.0f, t.localPosition.y);
            Assert.AreEqual(5.0f, t.localPosition.z);
        }

        [UnityTest]
        public IEnumerator T009_ChangeColorByPanel()
        {
            WorldObjectListItem woli = GetWOChooserItem(1);
            Transform t = woli.WorldObject.transform;

            woli.Test_OnPropertyPageClicked(); // Select the Test Cube's property page
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            panel.txt_Col_R.text = "0";
            panel.txt_Col_G.text = "1";
            panel.txt_Col_B.text = "0.50";

            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            Color col = Color.white;
            if (t.TryGetComponent(out Renderer renderer))
                col = renderer.material.color;

            Assert.AreEqual(0, col.r);
            Assert.AreEqual(1, col.g);
            Assert.AreEqual(0.50f, col.b);
        }

        [UnityTest]
        public IEnumerator T010_TransformModes()
        {
            GetWOChooserItem(1).Test_OnToChildClicked();     // Select first child, down a level
            yield return new WaitForEndOfFrame();

            GetWOChooserItem(1).Test_OnPropertyPageClicked(); // Select the Test Sphere's property page
            yield return new WaitForEndOfFrame();

            // Default: Local to Parent
            Assert.AreEqual("Test Sphere", panel.lbl_Heading.text);
            Assert.AreEqual("0.00", panel.txt_Pos_X.text);
            Assert.AreEqual("1.50", panel.txt_Pos_Y.text);
            Assert.AreEqual("0.00", panel.txt_Pos_Z.text);

            // Switch to Global
            panel.Test_SetLocalMode(false);
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            // Relative to origin (0, 0, 0) (= Test Cube -> Test Sphere)
            Assert.AreEqual("0.00", panel.txt_Pos_X.text);
            Assert.AreEqual("2.50", panel.txt_Pos_Y.text);
            Assert.AreEqual("5.00", panel.txt_Pos_Z.text);


            // Move the sphere
            panel.txt_Pos_X.text = "-1";
            panel.txt_Pos_Y.text = "3";
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            // Switch to Local
            panel.Test_SetLocalMode(true);
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            // Local coordinates reflect the movement
            Assert.AreEqual("-1.00", panel.txt_Pos_X.text);
            Assert.AreEqual("2.00", panel.txt_Pos_Y.text);
            Assert.AreEqual("0.00", panel.txt_Pos_Z.text);
        }

        [UnityTest]
        public IEnumerator T011_Remaning()
        {
            GetWOChooserItem(1).Test_OnToChildClicked();     // Select first child, down a level
            yield return new WaitForEndOfFrame();

            WorldObjectListItem woli = GetWOChooserItem(1);
            Assert.AreEqual("Test Sphere", woli.txt_Name.text);

            woli.txt_Name.text = "Test Sphere Renamed";
            yield return new WaitForEndOfFrame();

            Assert.AreEqual("Test Sphere Renamed" , woli.WorldObject.name);
        }
    }
#endif
}