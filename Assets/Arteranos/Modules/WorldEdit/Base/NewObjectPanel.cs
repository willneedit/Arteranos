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
using Arteranos.Core;

namespace Arteranos.WorldEdit
{
    public class NewObjectPanel : ActionPage
    {
        public event Action<WorldObjectInsertion> OnAddingNewObject;

        public void AddingNewObject(WorldObjectInsertion obj)
            => OnAddingNewObject?.Invoke(obj);
    }
}
