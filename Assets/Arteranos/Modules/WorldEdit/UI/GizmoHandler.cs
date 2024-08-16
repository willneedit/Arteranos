/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;

namespace Arteranos.WorldEdit
{
    public class GizmoHandler : MonoBehaviour
    {
        private void Update()
        {
            if(G.WorldEditorData.UsingGlobal)
                transform.rotation = Quaternion.identity;
            else
                transform.localRotation = Quaternion.identity;

            transform.localScale = new(
                1 / (transform.parent.lossyScale.x + float.Epsilon),
                1 / (transform.parent.lossyScale.y + float.Epsilon),
                1 / (transform.parent.lossyScale.z + float.Epsilon));
        }
    }
}
