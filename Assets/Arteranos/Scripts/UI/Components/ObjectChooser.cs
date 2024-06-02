/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using System;

namespace Arteranos.UI
{
    public class ObjectChooser : UIBehaviour
    {
        // [SerializeField] private GameObject grp_TileSample;

        [SerializeField] private TMP_Text lbl_PageCount;
        [SerializeField] private Button btn_First;
        [SerializeField] private Button btn_FRev;
        [SerializeField] private Button btn_Previous;
        [SerializeField] private Button btn_Next;
        [SerializeField] private Button btn_FFwd;
        [SerializeField] private Button btn_Last;

        [SerializeField] private TMP_InputField txt_AddItemURL;
        [SerializeField] private Button btn_AddItem;
        [SerializeField] private int ItemsPerPage = 2;

        [SerializeField] private GameObject TileBlueprint = null;


        public int currentPage { get; private set; } = 0;
        public int maxPage { get; private set; } = 0;

        // Page x is about to be shown. Recommend to call UpdateItemCount(), too.
        public event Action<int> OnShowingPage;

        // User requested to add a new item, entered in the text field.
        // No further actions are required, only to call ShowPage() if the page would be changed.
        public event Action<string> OnAddingItem;

        // The current tile is about to be populated. OK to invoke Coroutines if it's too
        // expensive to do.
        public event Action<int, GameObject> OnPopulateTile;

        private string pageCountPattern = null;
        private int itemCount = 0;



        protected override void Awake()
        {
            base.Awake();

            pageCountPattern = lbl_PageCount.text;

            btn_AddItem.onClick.AddListener(() =>
            {
                btn_AddItem.interactable = false;
                OnAddingItem?.Invoke(txt_AddItemURL.text);
            });

            btn_First.onClick.AddListener(() => SwitchToPage(0, -1));
            btn_FRev.onClick.AddListener(() => SwitchToPage(-10, 0));
            btn_Previous.onClick.AddListener(() => SwitchToPage(-1, 0));
            btn_Next.onClick.AddListener(() => SwitchToPage(1, 0));
            btn_FFwd.onClick.AddListener(() => SwitchToPage(10, 0));
            btn_Last.onClick.AddListener(() => SwitchToPage(0, 1));
        }

        protected override void Start()
        {
            base.Start();

            lbl_PageCount.text = "Loading...";

            // Intentionally no ShowPage() here. We still need to build up the
            // list in question right now.
        }

        public void UpdateItemCount(int count)
        {
            itemCount = count;
            maxPage = (itemCount + ItemsPerPage - 1) / ItemsPerPage;
        }

        public void FinishAdding()
        {
            btn_AddItem.interactable = true;
        }

        public GameObject GetChooserItem(int index)
        {
            Transform panels = transform.GetChild(1);

            return index < panels.childCount 
                ? panels.GetChild(index).gameObject 
                : null;
        }

        public void ShowPage(int currentPage)
        {
            OnShowingPage?.Invoke(currentPage);
            this.currentPage = currentPage;
            lbl_PageCount.text = string.Format(pageCountPattern, currentPage + 1, maxPage);

            Transform panels = transform.GetChild(1);

            for (int i = 0; i < panels.childCount; i++)
                Destroy(panels.GetChild(i).gameObject);

            int startIndex = currentPage * ItemsPerPage;
            int endIndex = startIndex + ItemsPerPage - 1;
            if (endIndex > itemCount) endIndex = itemCount;

            for (int i = startIndex; i < endIndex; i++)
            {
                GameObject go = Instantiate(TileBlueprint, panels);
                OnPopulateTile?.Invoke(i, go);
            }

            lbl_PageCount.text = string.Format(pageCountPattern, currentPage + 1, maxPage);

        }
        private void SwitchToPage(int difference, int location)
        {
            int newPage = location switch
            {
                < 0 => difference,
                0 => currentPage + difference,
                > 0 => maxPage - 1 - difference
            };

            if (newPage >= maxPage) newPage = maxPage - 1;
            else if (newPage < 0) newPage = 0;

            ShowPage(newPage);
        }

    }
}