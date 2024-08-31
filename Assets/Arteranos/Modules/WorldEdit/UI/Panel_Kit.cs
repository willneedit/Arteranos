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
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arteranos.WorldEdit
{
    public class Panel_Kit : NewObjectPanel
    {
        public List<WOCEntry> KitEntries { get; private set; } = null;

        public ObjectChooser Chooser;
        public GameObject bp_KitItemGO;

        private Client Client = null;
        private bool dirty = false;

        private GameObject KitItemGO = null;

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

            Client = G.Client;
            KitEntries = Client != null
                ? Client.WEAC.WorldObjectsKits
                : new();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (Client != null && dirty)
            {
                Client.WEAC.WorldObjectsKits = KitEntries;
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
            Chooser.UpdateItemCount(KitEntries.Count);
        }

        private void PopulateTile(int index, GameObject @object)
        {
            WOCEntry entry = KitEntries[index];
            IPFSImage image = @object.GetComponentInChildren<IPFSImage>();
            TMP_Text text = @object.GetComponentInChildren<TMP_Text>();
            Button button = @object.GetComponentInChildren<Button>();

            image.Path = $"{entry.IPFSPath}/Screenshot.png";
            text.text = entry.FriendlyName;

            button.onClick.AddListener(() => OnTileClicked(index));
        }

        private void RequestToAdd(string sourceURL)
        {
            IEnumerator Cor()
            {
                Chooser.Btn_AddItem.interactable = false;

                (AsyncOperationExecutor<Context> ao, Context co) =
                    AssetUploader.PrepareUploadToIPFS(sourceURL, true); // Kit Archive

                ao.ProgressChanged += (ratio, msg) => Chooser.Lbl_PageCount = $"{msg}";

                AggregateException ex = null;
                yield return ao.ExecuteCoroutine(co, (_status, _) => ex = _status);

                KitEntries.Add(new()
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
            WOKitItem newwOKitItem = new()
            {
                kitCid = KitEntries[index].IPFSPath,
                kitItemName = default
            };

            bp_KitItemGO.SetActive(false);

            if (!KitItemGO) KitItemGO = Instantiate(bp_KitItemGO, transform.parent);

            KitItemGO.TryGetComponent(out Panel_KitItem kitItem);
            kitItem.Item = newwOKitItem;
            kitItem.ParentPanel = this;

            KitItemGO.SetActive(true);
            gameObject.SetActive(false);
        }
    }
}
