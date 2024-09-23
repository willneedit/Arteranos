/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.WorldEdit.Components;
using UnityEngine;

namespace Arteranos.WorldEdit
{
    public interface IInspector
    {
        WOCBase Woc { get; set; }
        PropertyPanel PropertyPanel { get; set; }

        void Populate();
    }
}