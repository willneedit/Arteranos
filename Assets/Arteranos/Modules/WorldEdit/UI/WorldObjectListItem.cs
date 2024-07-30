/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;

namespace Arteranos.WorldEdit
{
    public class WorldObjectListItem : UIBehaviour
    {
        public Button btn_Lock;
        public Button btn_Unlock;
        public Button btn_Property;
        public Button btn_ToParent;
        public Button btn_ToChild;
        public TMP_InputField txt_Name;
        public Button btn_Delete;

        public GameObject WorldObject { get; set; }
        public bool IsParentLink { get; set; }
        public bool IsRoot {  get; set; }
        public WorldObjectList Container { get; set; }


        protected override void Awake()
        {
            base.Awake();

            btn_ToParent.onClick.AddListener(GotToParentClicked);
            btn_ToChild.onClick.AddListener(GotToChildClicked);
            btn_Lock.onClick.AddListener(() => SetLockState(true));
            btn_Unlock.onClick.AddListener(() => SetLockState(false));
            btn_Delete.onClick.AddListener(GotDeleteClicked);
            btn_Property.onClick.AddListener(GotPropertyPageClicked);

            txt_Name.onValueChanged.AddListener(GotChangedName);
        }

        protected override void Start()
        {
            base.Start();

            Populate();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        private void Populate()
        {
            bool IsLocked = false;
            if(WorldObject.TryGetComponent(out WorldObjectComponent asset))
                IsLocked = asset.IsLocked;

            if (IsRoot)
            {
                btn_Lock.gameObject.SetActive(false);
                btn_Unlock.gameObject.SetActive(false);
                btn_ToParent.gameObject.SetActive(false);
                btn_Property.gameObject.SetActive(false);
                btn_ToChild.gameObject.SetActive(false);
                txt_Name.text = "(Root)";
                txt_Name.interactable = false;
                btn_Delete.gameObject.SetActive(false);
                return;
            }

            btn_Lock.gameObject.SetActive(!IsLocked);
            btn_Unlock.gameObject.SetActive(IsLocked);
            btn_ToParent.gameObject.SetActive(IsParentLink);
            btn_ToChild.gameObject.SetActive(!IsParentLink);
            btn_Property.interactable = !IsLocked;
            txt_Name.interactable = !IsLocked;
            txt_Name.text = WorldObject.name;
            btn_Delete.interactable = !IsLocked && !IsParentLink;
        }

        private void GotToChildClicked()
        {
            Container.ChangeFolder(WorldObject);
        }

        private void GotToParentClicked()
        {
            Container.ChangeFolder(WorldObject.transform.parent.gameObject);
        }

        private void SetLockState(bool locked)
        {
            if (WorldObject.TryGetComponent(out WorldObjectComponent asset))
                asset.IsLocked = locked;

            Populate();
        }

        private void GotDeleteClicked()
        {
            WorldObjectDeletion wod = new();
            wod.SetPathFromThere(WorldObject.transform);

            wod.EmitToServer();
            WorldObject = null;
        }

        private void GotChangedName(string name) 
            => WorldObject.name = name;

        private void GotPropertyPageClicked() 
            => Container.SwitchToPropertyPage(this);


#if UNITY_EDITOR
        // Unit test backdoors
        public void Test_OnToChildClicked() => GotToChildClicked();
        public void Test_OnToParentClicked() => GotToParentClicked();
        public void Test_OnSetLockState(bool locked) => SetLockState(locked);
        public void Test_OnDeleteClicked() => GotDeleteClicked();
        public void Test_OnPropertyPageClicked() => GotPropertyPageClicked();
#endif
    }
}
