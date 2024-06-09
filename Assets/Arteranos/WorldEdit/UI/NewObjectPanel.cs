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

namespace Arteranos.WorldEdit
{
    public class NewObjectPanel : UIBehaviour
    {
        public event Action<WorldObject> OnAddingNewObject;

        protected void AddingNewObject(WorldObject obj)
            => OnAddingNewObject?.Invoke(obj);
    }
}
