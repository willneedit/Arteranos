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
using Arteranos.WorldEdit.Components;
using Arteranos.UI;

namespace Arteranos.WorldEdit
{
    public class PropertyPanel : UIBehaviour
    {
        public Button btn_ReturnToList;
        public TextMeshProUGUI lbl_Heading;

        public Transform grp_Component_List;

        public Transform grp_AddNewComponent;
        public Spinner spn_NewComponent;
        public Button btn_NewComponent;

        public GameObject bp_CollapsiblePane;

        public GameObject WorldObject
        {
            get => G.WorldEditorData.FocusedWorldObject;
            set
            {
                G.WorldEditorData.FocusedWorldObject = value;
                Woc = G.WorldEditorData.FocusedWorldObject.GetComponent<WorldObjectComponent>();
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

        private readonly Dictionary<string, WOCBase> OptionalComponents = new();

        protected override void Awake()
        {
            base.Awake();

            btn_ReturnToList.onClick.AddListener(GotReturnToChooserClick);
            btn_NewComponent.onClick.AddListener(GotNewComponentClick);

            WOCBase[] oclist =
            {
                new WOCTeleportMarker(),
                new WOCTeleportButton(),
                new WOCTeleportSurface(),
                new WOCSpawner(),
                new WOCRigidBody()
            };

            foreach(var o in oclist)
                OptionalComponents.Add(o.GetUI().name, o);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Transform root = WorldEditorData.FindObjectByPath(null);

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
            Action MakeGotRemoveComponentClicked(int index)
                => () => GotRemoveComponentClicked(index);

            grp_Component_List.gameObject.SetActive(false);

            // To prevent interference from to-be-destroyed gameobjects....
            // 1. Use a loop down.
            // 2. Explicitly set the objects inactive to prevent it being set active
            // 3. Then, detach the object from its parent
            // 4. Finally, set it to be destroyed.
            // Reason? Until the next frame update, there'd be leftovers in the hierarchy.
            for(int i = grp_Component_List.childCount - 1; i >= 0; i--)
            {
                GameObject oldPaneGO = grp_Component_List.GetChild(i).gameObject;
                oldPaneGO.TryGetComponent(out CollapsiblePane pane);

                paneOpen[pane.Title] = pane.IsOpen;

                oldPaneGO.SetActive(false);
                oldPaneGO.transform.SetParent(null);
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
                cp.IsDeleteable = wocc.IsRemovable;
                cp.OnDeleteClicked += MakeGotRemoveComponentClicked(i);

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

            grp_Component_List.gameObject.SetActive(true);
        }

        private void GotRemoveComponentClicked(int index)
        {
            WOCBase wocc = woc.WOComponents[index];
            Debug.Log($"Attempting to remove component {index}: {wocc}");
            woc.RemoveComponent(index);

            RebuildInspectors();

            RebuildMissingComponentsList();

            Populate();
        }

        private void Populate()
        {
            lbl_Heading.text = WorldObject.name;

            for (int i = 0; i < grp_Component_List.childCount; i++)
            {
                GameObject oldPaneGO = grp_Component_List.GetChild(i).gameObject;
                oldPaneGO.TryGetComponent(out CollapsiblePane pane);
                pane.EmbeddedWidget.TryGetComponent(out IInspector inspector);
                inspector?.Populate();
            }

            // Offer adding optional components, if there are some of them.
            RebuildMissingComponentsList();
        }

        public void CommitModification(IInspector i)
        {
            if(i.Woc.Dirty)
                WorldObject.MakePatch(false).EmitToServer();
        }

        private void GotReturnToChooserClick()
        {
            OnReturnToList?.Invoke();
        }

        private void RebuildMissingComponentsList()
        {
            bool hasInteractable = (woc.IsClickable != null) || (woc.IsGrabbable != null);

            List<string> missingCompons = new()
            {
                "Add new component..."
            };

            foreach(KeyValuePair<string, WOCBase> c in OptionalComponents)
            {
                bool isClickable = c.Value is IClickable;
                bool isGrabbable = c.Value is IRigidBody;

                // Only one potential interactable.
                if (hasInteractable && (isClickable || isGrabbable)) continue;

                if(!woc.TryGetWOC(out WOCBase _, c.Value.GetType()))
                    missingCompons.Add(c.Key);
            }

            spn_NewComponent.Options = missingCompons.ToArray();
            spn_NewComponent.value = 0;

            // Remove the entire widget group if there are none of them.
            grp_AddNewComponent.gameObject.SetActive(missingCompons.Count > 1);
        }

        private void GotNewComponentClick()
        {
            if (spn_NewComponent.value == 0) return;

            string newComponentName = spn_NewComponent.Options[spn_NewComponent.value];
            WOCBase newComponent = OptionalComponents[newComponentName];

            woc.AddOrReplaceComponent(newComponent);

            RebuildInspectors();

            RebuildMissingComponentsList();

            Populate();
        }

#if UNITY_EDITOR
        public void Test_OnReturnToChooserClicked() => GotReturnToChooserClick();
#endif
    }
}
