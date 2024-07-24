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
    public class Panel_glTF : NewObjectPanel
    {
        public List<WOCEntry> GLTFEntries { get; private set; } = null;

        public TMP_Text lbl_IPFSPath;
        public TMP_Text lbl_FriendlyName;

        public ObjectChooser Chooser;

        private Client Client = null;
        private bool dirty = false;

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

        protected override void OnEnable()
        {
            base.OnEnable();

            Client = SettingsManager.Client;
            if (Client != null)
                GLTFEntries = Client.WEAC.WorldObjectsGLTF;
            else
                GLTFEntries = new();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (Client != null && dirty)
            {
                Client.WEAC.WorldObjectsGLTF = GLTFEntries;
                Client.Save();
            }
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
            WOCEntry entry = GLTFEntries[index];
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
                Chooser.Btn_AddItem.interactable = false;

                (AsyncOperationExecutor<Context> ao, Context co) =
                    AssetUploader.PrepareUploadToIPFS(sourceURL, false); // Plain GLB file

                ao.ProgressChanged += (ratio, msg) => Chooser.Lbl_PageCount = $"{msg}";

                AggregateException ex = null;
                yield return ao.ExecuteCoroutine(co, (_status, _) => ex = _status);

                GLTFEntries.Add(new()
                {
                    IPFSPath = AssetUploader.GetUploadedCid(co),
                    FriendlyName = AssetUploader.GetUploadedFilename(co),
                });

                Chooser.Btn_AddItem.interactable = true;
                dirty = true;

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

            AddingNewObject(new()
            {
                asset = newWOglTF,
                name = GLTFEntries[index].FriendlyName
            });
        }
    }
}
