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

namespace Arteranos.WorldEdit
{
    public class WorldEditorUI : UIBehaviour
    {
        public WorldObjectList WorldObjectList;
        public PropertyPanel PropertyPanel;
        public GameObject NewObjectPicker;

        private NewObjectPanel[] NewObjectPanels = null;

        protected override void Start()
        {
            base.Start();

            WorldObjectList.gameObject.SetActive(true);
            PropertyPanel.gameObject.SetActive(false);
            NewObjectPicker.SetActive(false);

            WorldObjectList.OnWantsToAddItem += SwitchToAdder;
            WorldObjectList.OnWantsToModify += ModifyObject;
            PropertyPanel.OnReturnToList += SwitchToList;

            //ChoiceBook choiceBook = NewObjectPicker.GetComponent<ChoiceBook>();
            //NewObjectPanels = choiceBook.PaneList.GetComponentsInChildren<NewObjectPanel>(true);
            //foreach(NewObjectPanel panel in NewObjectPanels)
            //    panel.WorldEditorUI = this;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            WorldObjectList.OnWantsToAddItem -= SwitchToAdder;
            WorldObjectList.OnWantsToModify -= ModifyObject;
            PropertyPanel.OnReturnToList -= SwitchToList;
        }

        private void SwitchToAdder()
        {
            WorldObjectList.gameObject.SetActive(false);
            PropertyPanel.gameObject.SetActive(false);
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

        private void Panel_OnAddingNewObject(WorldObject obj)
        {
            SwitchToList();
            WorldObjectList.OnAddingWorldObject(obj);
        }

        private void SwitchToList()
        {
            WorldObjectList.gameObject.SetActive(true);
            PropertyPanel.gameObject.SetActive(false);
            NewObjectPicker.SetActive(false);
        }

        private void SwitchToProperty()
        {
            WorldObjectList.gameObject.SetActive(false);
            PropertyPanel.gameObject.SetActive(true);
            NewObjectPicker.SetActive(false);
        }

        private void ModifyObject(WorldObjectListItem item)
        {
            PropertyPanel.WorldObject = item.WorldObject;
            SwitchToProperty();
        }
    }
}