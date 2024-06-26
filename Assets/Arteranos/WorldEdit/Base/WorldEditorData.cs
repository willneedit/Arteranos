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
    public class WorldEditorData : MonoBehaviour
    {
        // Are we in the edit mode at all?
        public bool IsInEditMode
        {
            get => isInEditMode;
            set
            {
                bool old = isInEditMode;
                isInEditMode = value;
                if (old != value) NotifyEditorModeChanged();
            }
        }

        // Movement and rotation constraints
        public bool LockXAxis = false;
        public bool LockYAxis = false;
        public bool LockZAxis = false;
        public bool isInEditMode = false;

        public event Action<WorldChange> OnWorldChanged;
        public event Action<bool> OnEditorModeChanged;

        public void NotifyWorldChanged(WorldChange worldChange)
            => OnWorldChanged?.Invoke(worldChange);

        public void NotifyEditorModeChanged()
            => OnEditorModeChanged?.Invoke(IsInEditMode);

        public void DoApply(WorldChange worldChange)
        {
            IEnumerator Cor()
            {
                yield return worldChange.Apply();

                NotifyWorldChanged(worldChange);
            }

            StartCoroutine(Cor());
        }
    }
}