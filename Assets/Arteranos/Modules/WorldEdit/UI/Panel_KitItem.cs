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
using Ipfs.Unity;
using ProtoBuf;
using System.IO;

namespace Arteranos.WorldEdit
{
    public class Panel_KitItem : NewObjectPanel
    {
        public NewObjectPanel ParentPanel { get; set; } = null;
        public WOKitItem Item { get; set; } = null;
        public KitEntryList KitItemEntries { get; private set; } = default;

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

        protected override void OnEnable()
        {
            IEnumerator Cor()
            {
                byte[] map = null;
                yield return Asyncs.Async2Coroutine(
                    G.IPFSService.ReadBinary($"{Item.kitCid}/map"), _result => map = _result);

                KitItemEntries = Serializer.Deserialize<KitEntryList>(new MemoryStream(map));

                Chooser.ShowPage(0);
            }
            base.OnEnable();


            StartCoroutine(Cor());
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        protected override void Start()
        {
            base.Start();

            //Chooser.ShowPage(0);
        }

        private void PreparePage(int obj)
        {
            Chooser.UpdateItemCount(KitItemEntries.Items.Count);
        }

        private void PopulateTile(int index, GameObject @object)
        {
            KitEntryItem entry = KitItemEntries.Items[index];
            IPFSImage image = @object.GetComponentInChildren<IPFSImage>();
            TMP_Text text = @object.GetComponentInChildren<TMP_Text>();
            Button button = @object.GetComponentInChildren<Button>();

            image.Path = $"{Item.kitCid}/KitScreenshots/{entry.GUID}.png";
            text.text = entry.Name;

            button.onClick.AddListener(() => OnTileClicked(index));
        }

        private void OnTileClicked(int index)
        {
            Item.kitItemName = KitItemEntries.Items[index].GUID;

            // Defer to the kit selection panel, as this is registered by the choicebook
            ParentPanel.AddingNewObject(new()
            {
                asset = Item,
                name = KitItemEntries.Items[index].Name,
                components = new()
            });

            // And manually put it to sleep.
            gameObject.SetActive(false);
            ParentPanel.gameObject.SetActive(true);
        }
    }
}
