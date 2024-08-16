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
using System.Collections.Generic;

namespace Arteranos.WorldEdit
{
    public class PropertyPanel : UIBehaviour
    {
        public Button btn_ReturnToList;
        public TextMeshProUGUI lbl_Heading;

        public Toggle chk_Local;
        public Toggle chk_Global;
        public Slider sld_Hue;

        public Toggle chk_Collidable;

        public Transform grp_Component_List;

        public GameObject bp_CollapsiblePane;

        public GameObject WorldObject
        {
            get => G.WorldEditorData.CurrentWorldObject;
            set
            {
                G.WorldEditorData.CurrentWorldObject = value;
                Woc = G.WorldEditorData.CurrentWorldObject.GetComponent<WorldObjectComponent>();
            }
        }

        public WorldObjectComponent Woc
        {
            get => woc;
            private set
            {
                woc = value;
                RebuildInspectors();
            }
        }

        public event Action OnReturnToList;

        private WorldObjectComponent woc;

        protected override void Awake()
        {
            base.Awake();

            btn_ReturnToList.onClick.AddListener(GotReturnToChooserClick);
            chk_Local.onValueChanged.AddListener(_ => SetLocalMode(true));
            chk_Global.onValueChanged.AddListener(_ => SetLocalMode(false));

            chk_Collidable.onValueChanged.AddListener(SetCollidable);
        }

        private void SetCollidable(bool collidable) => Woc.IsCollidable = collidable;

        protected override void OnEnable()
        {
            base.OnEnable();

            Transform root = WorldEditorData.FindObjectByPath(null);

            SetLocalMode(!G.WorldEditorData.UsingGlobal);

            Populate();

            G.WorldEditorData.OnWorldChanged += GotWorldChanged;
        }

        private void GotWorldChanged(IWorldChange change)
        {
            // Skip if it isn't an object modification
            if (change is not WorldObjectPatch wop) return;

            // Same as with a different object.
            // Maybe another user fiddled with another object. Think collaborative editing.
            if (wop.path[^1] != Woc.Id) return;

            Populate();
        }

        protected override void OnDisable()
        {
            G.WorldEditorData.OnWorldChanged -= GotWorldChanged;

            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        private readonly Dictionary<string, bool> paneOpen = new();

        private void RebuildInspectors()
        {
            for(int i = 0; i < grp_Component_List.childCount; i++)
            {
                GameObject oldPaneGO = grp_Component_List.GetChild(i).gameObject;
                oldPaneGO.TryGetComponent(out CollapsiblePane pane);

                paneOpen[pane.Title] = pane.IsOpen;

                Destroy(oldPaneGO);
            }

            for (int i = 0; i < woc.WOComponents.Count; i++)
            {
                WOCBase wocc = woc.WOComponents[i];
                (string name, GameObject bp_contents) = wocc.GetUI();
                if (bp_contents == null) continue;

                GameObject paneGO = Instantiate(bp_CollapsiblePane, grp_Component_List);
                paneGO.TryGetComponent(out CollapsiblePane cp);

                cp.Title = name;
                cp.IsOpen = paneOpen.ContainsKey(cp.Title) && paneOpen[cp.Title];

                GameObject contentsGO = Instantiate(bp_contents, paneGO.transform);
                contentsGO.TryGetComponent(out IInspector inspector);
                if (inspector != null)
                {
                    inspector.Woc = wocc;
                    inspector.PropertyPanel = this;
                }
                else
                    Debug.LogWarning($"{name} doen't provide a valid inspector");
            }
        }


        private void Populate()
        {
            lbl_Heading.text = WorldObject.name;

            chk_Collidable.SetIsOnWithoutNotify(Woc.IsCollidable);

            for (int i = 0; i < grp_Component_List.childCount; i++)
            {
                GameObject oldPaneGO = grp_Component_List.GetChild(i).gameObject;
                oldPaneGO.TryGetComponent(out CollapsiblePane pane);
                pane.EmbeddedWidget.TryGetComponent(out IInspector inspector);
                inspector?.Populate();
            }
        }

        public void CommitModification(IInspector i)
        {
            if(i.Woc.Dirty)
                WorldObject.MakePatch(false).EmitToServer();
        }

        private void SetLocalMode(bool local)
        {
            chk_Local.SetIsOnWithoutNotify(local);
            chk_Global.SetIsOnWithoutNotify(!local);

            G.WorldEditorData.UsingGlobal = !local;

            Populate();
        }

        private void GotReturnToChooserClick()
        {
            OnReturnToList?.Invoke();
        }

#if UNITY_EDITOR
        public void Test_OnReturnToChooserClicked() => GotReturnToChooserClick();
        public void Test_SetLocalMode(bool local) => SetLocalMode(local);
#endif
    }
}
