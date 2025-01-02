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
    public class WOCPhysics : WOCBase
    {
        [ProtoMember(1)]
        public bool Collidable;
        [ProtoMember(2)]
        public bool Grabbable;
        [ProtoMember(3)]
        public bool ObeysGravity;
        [ProtoMember(4)]
        public float ResetDuration;

        private WorldObjectComponent woc = null;

        public override GameObject GameObject
        {
            get => base.GameObject;
            set
            {
                base.GameObject = value;
                GameObject.TryGetComponent(out woc);
            }
        }

        public override void CommitState()
        {
            base.CommitState();

            woc.UpdatePhysicsState();

            Dirty = false;
        }

        public void SetState()
        {
            Dirty = true;
        }

        public override object Clone()
        {
            return MemberwiseClone();
        }

        public override void ReplaceValues(WOCBase wOCBase)
        {
            WOCPhysics p = wOCBase as WOCPhysics;

            Collidable = p.Collidable;
            Grabbable = p.Grabbable;
            ObeysGravity = p.ObeysGravity;
            ResetDuration = p.ResetDuration;
        }
    }
}