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
    public class WorldObjectList : UIBehaviour
    {
        public event Action OnWantsToAddItem;
        public event Action<WorldObjectListItem> OnWantsToModify;

        private ObjectChooser Chooser = null;

        private GameObject WORoot = null;
        private GameObject CurrentRoot = null;

        protected override void Awake()
        {
            base.Awake();

            Chooser = GetComponent<ObjectChooser>();

            Chooser.OnShowingPage += PreparePage;
            Chooser.OnPopulateTile += PopulateTile;
            Chooser.OnAddingItem += RequestToAdd;

            WORoot = WorldEditorData.FindObjectByPath(null).gameObject;

            G.WorldEditorData.OnWorldChanged += GotWorldChanged;
            G.WorldEditorData.IsInEditMode = true;
        }

        private void GotWorldChanged(IWorldChange change)
        {
            // TODO Filter out changes which are invisible in the current list of objects
            RequestUpdateList();
        }

        protected override void OnDestroy()
        {
            Chooser.OnPopulateTile -= PopulateTile;
            Chooser.OnAddingItem -= RequestToAdd;
            Chooser.OnShowingPage -= PreparePage;

            G.WorldEditorData.IsInEditMode = false;
            G.WorldEditorData.OnWorldChanged -= GotWorldChanged;

            base.OnDestroy();
        }

        protected override void Start()
        {
            base.Start();

            CurrentRoot = WORoot;

            Chooser.ShowPage(0);
        }

        public void ChangeFolder(GameObject folder)
        {
            CurrentRoot = folder;
            Chooser.ShowPage(0);
        }

        private readonly List<GameObject> WorldObjects = new();

        private void PreparePage(int _ /* page index */)
        {
            WorldObjects.Clear();

            // The current root has vanished. Maybe because of undo or concurrent editing.
            if (!CurrentRoot) 
                CurrentRoot = WORoot;

            WorldObjects.Add( CurrentRoot );

            for(int i = 0; i < CurrentRoot.transform.childCount; ++i)
            {
                GameObject go = CurrentRoot.transform.GetChild(i).gameObject;
                if (go == null) continue;

                // Alien (aka unmanaged world objects) needs to be skipped.
                if (!go.TryGetComponent(out WorldObjectComponent _)) continue;
                WorldObjects.Add(go);
            }

            Chooser.UpdateItemCount(WorldObjects.Count);
        }

        private void PopulateTile(int index, GameObject @object)
        {
            bool isParent = false;
            bool isRoot = false;

            if (!@object.TryGetComponent(out WorldObjectListItem woli)) return;

            if(index == 0)
            {
                if (CurrentRoot != WORoot)
                    isParent = true;
                else
                    isRoot = true;
            }

            woli.IsParentLink = isParent;
            woli.IsRoot = isRoot;
            woli.WorldObject = WorldObjects[index];

            woli.Container = this;
        }

        public void RequestUpdateList()
        {
            Chooser.ShowPage(Chooser.CurrentPage);
        }

        public void SwitchToPropertyPage(WorldObjectListItem woli)
        {
            OnWantsToModify?.Invoke( woli );
        }

        private void RequestToAdd(string obj)
        {
            OnWantsToAddItem?.Invoke();
        }

        public void OnAddingWorldObject(WorldObjectInsertion woi)
        { 
            // Put the new object into the greater picture.
            // Path in its hierarchy....
            Transform current = CurrentRoot.transform;
            woi.SetPathFromThere(current);

            Transform ct = Camera.main.transform;

            // TODO Custom up vector?
            Vector3 facingAngles = new(
            0,
            ct.rotation.eulerAngles.y,
            0);

            // Add default components....

            // ...Transform as the first component, and place it in front of the user
            WOCTransform newTransform;

            if(CurrentRoot == WORoot)
            {
                // Root resides at 0,0,0, global = local
                newTransform = new WOCTransform()
                {
                    position = ct.position + ct.rotation * Vector3.forward * 2.5f,
                    rotation = facingAngles,
                    scale = Vector3.one
                };
            }
            else
            {
                // leaf sprouting off right on its parent
                newTransform = new WOCTransform()
                {
                    position = Vector3.zero,
                    rotation = Vector3.zero,
                    scale = Vector3.one
                };
            }

            woi.components.Insert(0, newTransform);

            // ...Physics component
            woi.components.Insert(1, new WOCPhysics());

            // Post the insertion request to the server, it should come back.
            // If you have the rights to do this.
            woi.EmitToServer();

            // Finished with adding.
            Chooser.Btn_AddItem.interactable = true;
        }
    }
}
