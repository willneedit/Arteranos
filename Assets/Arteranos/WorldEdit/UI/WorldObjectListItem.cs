/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Unity.Plastic.Newtonsoft.Json.Serialization;

namespace Arteranos
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
        public bool IsLocked { get; set; }
        public bool IsParentLink { get; set; }
        public bool IsRoot {  get; set; }

        // Requesting to change the list (adding/deleting items), not just the single item
        public event Action OnRequestUpdateList;

        protected override void Awake()
        {
            base.Awake();
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

        public void Populate()
        {
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
            btn_ToChild.gameObject.SetActive(transform.childCount > 0);
            txt_Name.text = WorldObject.name;
            btn_Delete.interactable = !IsLocked;
        }
    }
}
