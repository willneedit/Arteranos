/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Arteranos.WorldEdit
{
    public enum WorldEditMode
    {
        Translation = 0,
        Rotation,
        Scale
    }

    public interface IWorldEditorData
    {
        bool IsInEditMode { get; set; }
        bool LockXAxis { get; set; }
        bool LockYAxis { get; set; }
        bool LockZAxis { get; set; }
        bool UsingGlobal { get; set; }

        event Action<bool> OnEditorModeChanged;
        event Action<IWorldChange> OnWorldChanged;

        void AddBlueprint(IWorldObjectAsset woa, GameObject gameObject);
        void BuilderRequestedRedo();
        void BuilderRequestsUndo();
        IEnumerator BuildWorld(IWorldDecoration worldDecoration);
        void ClearBlueprints();
        IWorldDecoration DeserializeWD(Stream stream);
        void DoApply(Stream stream);
        void DoApply(IWorldChange worldChange);
        void NotifyEditorModeChanged();
        void NotifyWorldChanged(IWorldChange worldChange);
        IEnumerator RecallUndoState(string hash);
        IWorldDecoration TakeSnapshot();
        bool TryGetBlueprint(IWorldObjectAsset woa, out GameObject gameObject);
    }
}