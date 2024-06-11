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
using Arteranos.UI;
using UnityEngine.Events;

namespace Arteranos.WorldEdit
{

    public class Panel_glTF : NewObjectPanel
    {
        public TMP_Text lbl_IPFSPath;
        public TMP_Text lbl_FriendlyName;

        public ObjectChooser Chooser;

        protected override void Awake()
        {
            base.Awake();

            Chooser.OnShowingPage += PreparePage;
            Chooser.OnPopulateTile += PopulateTile;
            Chooser.OnAddingItem += RequestToAdd;
        }

        protected override void OnDestroy()
        {
            Chooser.OnShowingPage -= PreparePage;
            Chooser.OnPopulateTile -= PopulateTile;
            Chooser.OnAddingItem -= RequestToAdd;

            base.OnDestroy();
        }

        protected override void Start()
        {
            base.Start();

            Chooser.ShowPage(0);
        }

        private void PreparePage(int obj)
        {
            throw new NotImplementedException();
        }

        private void PopulateTile(int arg1, GameObject @object)
        {
            throw new NotImplementedException();
        }

        private void RequestToAdd(string obj)
        {
            throw new NotImplementedException();
        }
    }
}
