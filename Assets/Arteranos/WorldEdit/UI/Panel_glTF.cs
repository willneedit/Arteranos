/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;
using TMPro;
using System;
using Arteranos.UI;
using System.Collections;
using Arteranos.Core;
using Arteranos.Core.Operations;
using System.Collections.Generic;

namespace Arteranos.WorldEdit
{
    public struct GlTFObjectsEntry
    {
        public string IPFSPath;
        public string FriendlyName;
    }

    public class Panel_glTF : NewObjectPanel
    {
        public List<GlTFObjectsEntry> GLTFEntries { get; private set; } = new();

        public TMP_Text lbl_IPFSPath;
        public TMP_Text lbl_FriendlyName;

        public ObjectChooser Chooser;

        // TODO On Awake: Retrieve the glTF gallery list from the user settings
        // TODO On Disable and dirty list: Save the glTF gallery list back to the user settings
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
            Chooser.UpdateItemCount(GLTFEntries.Count);
        }

        private void PopulateTile(int index, GameObject @object)
        {
            GlTFObjectsEntry entry = GLTFEntries[index];
            lbl_IPFSPath.text = entry.IPFSPath;
            lbl_FriendlyName.text = entry.FriendlyName;

            if (!@object.TryGetComponent(out GlTFChooserTile tile)) return;

            tile.GLTFObjectPath = entry.IPFSPath;
            tile.btn_PaneButton.onClick.AddListener(() => OnTileClicked(index));
        }

        private void RequestToAdd(string sourceURL)
        {
            IEnumerator Cor()
            {
                Chooser.btn_AddItem.interactable = false;

                (AsyncOperationExecutor<Context> ao, Context co) =
                    AssetUploader.PrepareUploadToIPFS(sourceURL, false); // Plain GLB file

                ao.ProgressChanged += (ratio, msg) => Chooser.lbl_PageCount.text = $"{msg}";

                AggregateException ex = null;
                yield return ao.ExecuteCoroutine(co, (_status, _) => ex = _status);

                GLTFEntries.Add(new()
                {
                    IPFSPath = AssetUploader.GetUploadedCid(co),
                    FriendlyName = AssetUploader.GetUploadedFilename(co),
                });

                Chooser.btn_AddItem.interactable = true;

                Chooser.ShowPage(0);
            }

            StartCoroutine(Cor());
        }
        private void OnTileClicked(int index)
        {
            WOglTF newWOglTF = new()
            { 
                glTFCid = GLTFEntries[index].IPFSPath 
            };

            AddingNewObject(new WorldObject(newWOglTF, GLTFEntries[index].FriendlyName));
        }
    }
}
