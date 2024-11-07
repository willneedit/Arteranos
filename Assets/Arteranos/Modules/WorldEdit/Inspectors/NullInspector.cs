/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.UI;
using Arteranos.WorldEdit.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arteranos.WorldEdit
{
    // Inspector for components without parameters
    public class NullInspector : UIBehaviour, IInspector
    {
        public WOCBase Woc { get; set; }
        public PropertyPanel PropertyPanel { get; set; }
        public void Populate() { }
    }
}
