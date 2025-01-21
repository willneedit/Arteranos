/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using ProtoBuf;
using UnityEngine;

namespace Arteranos.WorldEdit.Components
{

    [ProtoContract]
    public class WOCTeleportMarker : WOCBase, IPhysicsWOC
    {
        public override bool IsRemovable => false;

        public void SetState()
        {
            Dirty = true;
        }

        public override object Clone()
        {
            return MemberwiseClone();
        }

        public override void ReplaceValues(WOCBase wOCBase) { }

        public void UpdatePhysicsState(bool isInEditMode)
        {
            foreach (Renderer renderer in GameObject.GetComponentsInChildren<Renderer>())
                renderer.enabled = isInEditMode;
        }
    }
}