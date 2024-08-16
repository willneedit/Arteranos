/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using ProtoBuf;
using UnityEngine;

namespace Arteranos.WorldEdit
{
    [ProtoContract]
    public class WOCColor : WOCBase
    {
        [ProtoMember(1)]
        public WOColor color = Color.white;

        private Renderer renderer = null;

        public override GameObject GameObject
        {
            get => base.GameObject;
            set
            {
                base.GameObject = value;
                GameObject.TryGetComponent(out renderer);
            }
        }

        public override void CommitState()
        {
            base.CommitState();

            if(renderer != null)
                renderer.material.color = color;
        }

        public override void CheckState()
        {
            Dirty = false;
            if(renderer != null && renderer.material.color != color)
                Dirty = true;
        }

        public void SetState(Color color)
        {
            this.color = color;

            CheckState();
        }

        public override object Clone()
        {
            return MemberwiseClone();
        }

        public override (string name, GameObject gameObject) GetUI()
            => ("Color", BP.I.WorldEdit.ColorInspector);

        public override void ReplaceValues(WOCBase wOCBase)
        {
            WOCColor c = wOCBase as WOCColor;

            color = c.color;
        }
    }
}
