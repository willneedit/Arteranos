/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;
using TMPro;
using Arteranos.UI;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Arteranos.WorldEdit
{
    public class Panel_KitItem : NewObjectPanel
    {
        public Kit Item { get; set; } = null;
        public Dictionary<Guid, string> KitItemEntries { get; private set; } = default;
        public List<Guid> guids { get; private set; } = new();

        public ObjectChooser Chooser;

        protected override void Awake()
        {
            base.Awake();

            Chooser.OnShowingPage += PreparePage;
            Chooser.OnPopulateTile += PopulateTile;
        }

        protected override void OnDestroy()
        {
            Chooser.OnShowingPage -= PreparePage;
            Chooser.OnPopulateTile -= PopulateTile;

            base.OnDestroy();
        }

        private void PreparePage(int obj)
        {
            Chooser.UpdateItemCount(guids.Count);
        }

        private void PopulateTile(int index, GameObject @object)
        {
            IEnumerator Cor()
            {
                Guid guid = guids[index];

                IPFSImage image = @object.GetComponentInChildren<IPFSImage>();
                TMP_Text text = @object.GetComponentInChildren<TMP_Text>();
                Button button = @object.GetComponentInChildren<Button>();

                Core.AsyncLazy<byte[]> ItemScreenshotPNG = Item.ItemScreenshotPNGs[guid];

                yield return ItemScreenshotPNG.WaitFor();

                image.ImageData = ItemScreenshotPNG.Result;
                text.text = KitItemEntries[guid];

                button.onClick.AddListener(() => OnTileClicked(index));
            }

            StartCoroutine(Cor());
        }

        private void OnTileClicked(int index)
        {
            Guid guid = guids[index];
            WOKitItem asset = new()
            {
                kitCid = Item.RootCid,
                kitItemName = guid
            };

            // Defer to the kit selection panel, as this is registered by the choicebook
            BackOut(new WorldObjectInsertion()
            {
                asset = asset,
                name = KitItemEntries[guid],
                components = new()
            });
        }

        public override void Called(object data)
        {
            base.Called(data);

            Item = data as Kit;

            IEnumerator Cor()
            {
                yield return Item.ItemMap.WaitFor();

                KitItemEntries = Item.ItemMap.Result;

                guids = KitItemEntries.Keys.ToList();

                Chooser.ShowPage(0);
            }

            StartCoroutine(Cor());
        }
    }
}
