/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using ProtoBuf;
using UnityEngine;

namespace Arteranos.WorldEdit.Components
{

    [ProtoContract]
    public class WOCSpawnPoint : WOCBase, IPhysicsWOC
    {
        public void SetState()
        {
            Dirty = true;
        }

        public override object Clone()
        {
            return MemberwiseClone();
        }

        public override (string name, GameObject gameObject) GetUI()
            => ("Spawn Point", BP.I.WorldEdit.NullInspector);

        public override void ReplaceValues(WOCBase wOCBase) { }

        public void UpdatePhysicsState(bool isInEditMode)
        {
            if (!GameObject.TryGetComponent(out User.SpawnPoint area))
                area = GameObject.AddComponent<User.SpawnPoint>();

            GameObject.SetActive(isInEditMode);
        }
    }
}
