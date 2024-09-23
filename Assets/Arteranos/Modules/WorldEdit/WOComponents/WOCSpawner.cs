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
    public class WOCSpawner : WOCBase
    {
        [ProtoMember(1)]
        public int MaxItems;
        [ProtoMember(2)]
        public WOVector3 Force;
        [ProtoMember(3)]
        public float Lifetime;

        public void SetState()
        {
            Dirty = true;
        }

        public override object Clone()
        {
            return MemberwiseClone();
        }

        public override (string name, GameObject gameObject) GetUI()
            => ("Spawner", BP.I.WorldEdit.SpawnerInspector);

        public override void ReplaceValues(WOCBase wOCBase)
        {
            WOCSpawner s = wOCBase as WOCSpawner;

            MaxItems = s.MaxItems;
            Force = s.Force;
            Lifetime = s.Lifetime;
        }
    }
}