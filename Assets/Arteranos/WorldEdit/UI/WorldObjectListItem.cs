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

            btn_ToParent.onClick.AddListener(OnToParentClicked);
            btn_ToChild.onClick.AddListener(OnToChildClicked);
            btn_Lock.onClick.AddListener(() => OnSetLockState(true));
            btn_Unlock.onClick.AddListener(() => OnSetLockState(false));
            btn_Delete.onClick.AddListener(OnDeleteClicked);
            btn_Property.onClick.AddListener(OnPropertyPageClicked);

            txt_Name.onValueChanged.AddListener(OnChangedName);
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

        private void OnToChildClicked()
        {
            Container.ChangeFolder(WorldObject);
        }

        private void OnToParentClicked()
        {
            Container.ChangeFolder(WorldObject.transform.parent.gameObject);
        }

        private void OnSetLockState(bool locked)
        {
            if (WorldObject.TryGetComponent(out WorldObjectComponent asset))
                asset.IsLocked = locked;

            Populate();
        }

        private void OnDeleteClicked()
        {
            // Unhook this object from the hierarchy
            WorldObject.transform.SetParent(null);
            Container.RequestUpdateList();

            // Slate it for destruction
            Destroy(WorldObject); 
            WorldObject = null;
        }

        private void OnChangedName(string name) 
            => WorldObject.name = name;

        private void OnPropertyPageClicked() 
            => Container.SwitchToPropertyPage(this);


#if UNITY_EDITOR
        // Unit test backdoors
        public void Test_OnToChildClicked() => OnToChildClicked();
        public void Test_OnToParentClicked() => OnToParentClicked();
        public void Test_OnSetLockState(bool locked) => OnSetLockState(locked);
        public void Test_OnDeleteClicked() => OnDeleteClicked();
        public void Test_OnPropertyPageClicked() => OnPropertyPageClicked();
#endif
    }
}
