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
    public class WOCTeleportMarker : WOCBase
    {
        [ProtoMember(1)]
        public string location = null;

        public void SetState()
        {
            Dirty = true;
        }

        public override object Clone()
        {
            return MemberwiseClone();
        }

        public override (string name, GameObject gameObject) GetUI()
            => ("Teleport Marker", BP.I.WorldEdit.TeleportMarkerInspector);

        public override void ReplaceValues(WOCBase wOCBase)
        {
            WOCTeleportMarker s = wOCBase as WOCTeleportMarker;

            s.location = location;
        }
    }
}