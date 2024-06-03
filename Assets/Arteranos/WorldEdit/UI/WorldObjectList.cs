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
        [SerializeField] private ObjectChooser Chooser;

        private GameObject WORoot = null;
        private GameObject CurrentRoot = null;

        protected override void Awake()
        {
            base.Awake();

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
            if( WORoot == null )
            {
                WORoot = new("World Objects Root");
                WORoot.tag = "WorldObjectsRoot";
            }

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
            Chooser.ShowPage(Chooser.currentPage);
        }

        private void RequestToAdd(string obj)
        {
            throw new NotImplementedException();
        }



    }


}
