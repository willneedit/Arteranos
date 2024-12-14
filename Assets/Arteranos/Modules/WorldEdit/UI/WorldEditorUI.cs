/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;

using Arteranos.UI;
using UnityEngine.UI;
using Arteranos.Core;

namespace Arteranos.WorldEdit
{
    public class WorldEditorUI : ActionPage
    {
        public WorldObjectList WorldObjectList;
        public PropertyPanel PropertyPanel;
        public NewObjectPicker NewObjectPicker;
        public SaveWorldPanel SaveWorldPanel;

        public Button btn_UILock;
        public Button btn_UIUnlock;
        public Button btn_AddNew;
        public Button btn_Undo;
        public Button btn_Redo;
        public Button btn_Paste;
        public Button btn_Save;

        private CameraUITracker Tracker = null;

        protected override void Start()
        {
            base.Start();

            Tracker = GetComponentInParent<CameraUITracker>();

            WorldObjectList.gameObject.SetActive(true);

            btn_UILock.onClick.AddListener(() => SetUILockState(true));
            btn_UIUnlock.onClick.AddListener(() => SetUILockState(false));

            btn_AddNew.onClick.AddListener(SwitchToAdder);
            btn_Undo.onClick.AddListener(G.WorldEditorData.BuilderRequestsUndo);
            btn_Redo.onClick.AddListener(G.WorldEditorData.BuilderRequestedRedo);
            btn_Paste.onClick.AddListener(GotPasteClicked);
            btn_Save.onClick.AddListener(SwitchToSave);

            WorldObjectList.OnWantsToModify += ModifyObject;

            SetUILockState(false);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            WorldObjectList.OnWantsToModify -= ModifyObject;
        }

        public override void OnEnterLeaveAction(bool onEnter)
        {
            WorldObjectList.gameObject.SetActive(onEnter);
        }

        private void SetUILockState(bool locking)
        {
            btn_UILock.gameObject.SetActive(!locking);
            btn_UIUnlock.gameObject.SetActive(locking);

            if(Tracker) Tracker.enabled = !locking;
        }

        private void SwitchToAdder()
        {
            ActionRegistry.Call(
                "embedded.worldEditor.addNew", 
                callTo: NewObjectPicker,
                callback: Panel_OnAddingNewObject);
        }

        private void SwitchToSave()
        {
            ActionRegistry.Call(
                "embedded.worldEditor.save",
                callTo: SaveWorldPanel);
        }

        private void ModifyObject(WorldObjectListItem item)
        {
            ActionRegistry.Call(
                "embedded.worldEditor.properties",
                data: item.WorldObject,
                callTo: PropertyPanel);
        }

        private void Panel_OnAddingNewObject(object obj)
        {
            if (obj == null) return;
            WorldObjectList.OnAddingWorldObject(obj as WorldObjectInsertion);
        }

        private void GotPasteClicked()
        {
            G.WorldEditorData.RecallFromPasteBuffer(WorldObjectList.CurrentRoot.transform);
        }
    }
}
