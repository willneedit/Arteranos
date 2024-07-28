/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

using Arteranos.UI;
using System;
using UnityEngine.UI;

namespace Arteranos.WorldEdit
{
    public class WorldEditorUI : UIBehaviour
    {
        public WorldObjectList WorldObjectList;
        public PropertyPanel PropertyPanel;
        public GameObject NewObjectPicker;
        public SaveWorldPanel SaveWorldPanel;

        public Button btn_AddNew;
        public Button btn_Undo;
        public Button btn_Redo;
        public Button btn_Save;

        private NewObjectPanel[] NewObjectPanels = null;

        protected override void Start()
        {
            base.Start();

            WorldObjectList.gameObject.SetActive(true);
            PropertyPanel.gameObject.SetActive(false);
            NewObjectPicker.SetActive(false);

            btn_AddNew.onClick.AddListener(SwitchToAdder);
            btn_Undo.onClick.AddListener(G.WorldEditorData.BuilderRequestsUndo);
            btn_Redo.onClick.AddListener(G.WorldEditorData.BuilderRequestedRedo);
            btn_Save.onClick.AddListener(SwitchToSave);

            WorldObjectList.OnWantsToModify += ModifyObject;
            PropertyPanel.OnReturnToList += SwitchToList;
            SaveWorldPanel.OnReturnToList += SwitchToList;

            // Editing world without the template is just for testing in the Unity Editor.
            btn_Save.interactable = (G.World.Cid != null);

            //ChoiceBook choiceBook = NewObjectPicker.GetComponent<ChoiceBook>();
            //NewObjectPanels = choiceBook.PaneList.GetComponentsInChildren<NewObjectPanel>(true);
            //foreach(NewObjectPanel panel in NewObjectPanels)
            //    panel.WorldEditorUI = this;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            WorldObjectList.OnWantsToModify -= ModifyObject;
            PropertyPanel.OnReturnToList -= SwitchToList;
            SaveWorldPanel.OnReturnToList -= SwitchToList;
        }

        private void SwitchToAdder()
        {
            WorldObjectList.gameObject.SetActive(false);
            PropertyPanel.gameObject.SetActive(false);
            SaveWorldPanel.gameObject.SetActive(false);
            NewObjectPicker.SetActive(true);

            ChoiceBook choiceBook = NewObjectPicker.GetComponent<ChoiceBook>();
            NewObjectPanels = choiceBook.PaneList.GetComponentsInChildren<NewObjectPanel>(true);
            foreach (NewObjectPanel panel in NewObjectPanels)
            {
                // Prevent double entries, maybe...?
                panel.OnAddingNewObject -= Panel_OnAddingNewObject;
                panel.OnAddingNewObject += Panel_OnAddingNewObject;
            }
        }

        private void Panel_OnAddingNewObject(WorldObjectInsertion obj)
        {
            SwitchToList();
            WorldObjectList.OnAddingWorldObject(obj);
        }

        private void SwitchToList()
        {
            WorldObjectList.gameObject.SetActive(true);
            PropertyPanel.gameObject.SetActive(false);
            SaveWorldPanel.gameObject.SetActive(false);
            NewObjectPicker.SetActive(false);
        }

        private void SwitchToProperty()
        {
            WorldObjectList.gameObject.SetActive(false);
            PropertyPanel.gameObject.SetActive(true);
            SaveWorldPanel.gameObject.SetActive(false);
            NewObjectPicker.SetActive(false);
        }

        private void SwitchToSave()
        {
            WorldObjectList.gameObject.SetActive(false);
            PropertyPanel.gameObject.SetActive(false);
            SaveWorldPanel.gameObject.SetActive(true);
            NewObjectPicker.SetActive(false);
        }

        private void ModifyObject(WorldObjectListItem item)
        {
            PropertyPanel.WorldObject = item.WorldObject;
            SwitchToProperty();
        }
    }
}
