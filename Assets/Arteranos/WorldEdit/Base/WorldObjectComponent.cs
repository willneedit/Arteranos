/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using System.Collections.Generic;
using ProtoBuf;

using Arteranos.Core;
using UnityEngine;
using System.IO;
using System;

namespace Arteranos.WorldEdit
{
    // Just for keeping the data for converting the gameobject to a serializable world object.
    public class WorldObjectComponent : MonoBehaviour
    {
        public WorldObjectAsset Asset { get; set; } = null;
        public List<WOCBase> WOComponents { get; set; } = null;
        public bool IsLocked { get; set; } = false;

        public event Action OnStateChanged;

        private void Update()
        {
            bool dirty = false;
            foreach(WOCBase w in WOComponents)
            {
                w.Update();
                dirty |= w.Dirty;
            }


            if (dirty) TriggerStateChanged();
        }

        /// <summary>
        /// Notify obserevers about the changed state
        /// </summary>
        public void TriggerStateChanged()
        {
            OnStateChanged?.Invoke();
        }

        public bool TryGetWOC<T>(out T woComponent) where T : WOCBase
        {
            foreach (WOCBase w in WOComponents)
                if(w is T woc)
                {
                    woComponent = woc;
                    return true;
                }
            woComponent = null;
            return false;
        }
    }
}