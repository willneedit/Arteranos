/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using UnityEngine;
using UnityEngine.UI;

namespace Arteranos.UI
{
    public interface IObjectChooser
    {
        int CurrentPage { get; }
        int MaxPage { get; }
        string Lbl_PageCount { get; set; }
        string Txt_AddItemURL { get; set; }
        Button Btn_AddItem { get; set; }

        event Action<string> OnAddingItem;
        event Action<int, GameObject> OnPopulateTile;
        event Action<int> OnShowingPage;

        void FinishAdding();
        GameObject GetChooserItem(int index);
        void ShowPage(int currentPage);
        void UpdateItemCount(int count);
    }
}