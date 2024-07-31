/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using UnityEngine;

namespace Arteranos.WorldEdit
{
    public interface IWorldChange
    {
        IEnumerator Apply();
        void EmitToServer();
        void SetPathFromThere(Transform t);
    }
}