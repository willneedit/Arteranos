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
        private WorldEditorData EditorData = null;

        protected override void Awake()
        {
            base.Awake();

            Chooser = GetComponent<ObjectChooser>();

            Chooser.OnShowingPage += PreparePage;
            Chooser.OnPopulateTile += PopulateTile;
            Chooser.OnAddingItem += RequestToAdd;
        }

        protected override void OnDestroy()
        {
            Chooser.OnPopulateTile -= PopulateTile;
            Chooser.OnAddingItem -= RequestToAdd;
            Chooser.OnShowingPage -= PreparePage;

            base.OnDestroy();
        }

        protected override void Start()
        {
            base.Start();

            WORoot = GameObject.FindGameObjectWithTag("WorldObjectsRoot");
            WORoot.TryGetComponent(out EditorData);
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

        public void OnAddingWorldObject(WorldObject wo)
        { 
            IEnumerator AdderCoroutine()
            {
                yield return wo.Instantiate(CurrentRoot.transform, editorData: EditorData);

                RequestUpdateList();

                // Finished with adding.
                Chooser.btn_AddItem.interactable = true;
            }

            StartCoroutine(AdderCoroutine());
        }

    }


}
