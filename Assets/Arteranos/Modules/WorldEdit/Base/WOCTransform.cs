﻿/*
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
    public class WOCTransform : WOCBase
    {
        [ProtoMember(1)]
        public WOVector3 position = Vector3.zero;
        [ProtoMember(2)]
        public WOVector3 rotation = Vector3.zero; // Euler angles -- less data, needs Quaternion.eulerAngles and Quaternion.Euler()
        [ProtoMember(3)]
        public WOVector3 scale = Vector3.one;

        private Transform transform = null;

        public override GameObject GameObject
        {
            get => base.GameObject;
            set
            {
                base.GameObject = value;
                transform = GameObject.transform;
            }
        }

        public override void CommitState()
        {
            base.CommitState();

            transform.SetLocalPositionAndRotation(position, Quaternion.Euler(rotation));
            transform.localScale = scale;
        }

        public override void CheckState()
        {
            Dirty = false;
            if (transform.localPosition != position)
                Dirty = true;
            if(transform.localRotation != Quaternion.Euler(rotation))
                Dirty = true;
            if (transform.localScale != scale)
                Dirty = true;
        }

        public void SetState(Vector3 position, Vector3 rotation, Vector3 scale, bool global = false)
        {
            if(!global)
            {
                this.position = position;
                this.rotation = rotation;
                this.scale = scale;
            }
            else
            {
                Transform parent = transform.parent;
                Vector3 p_position = parent != null ? parent.position : Vector3.zero;
                Quaternion p_rotation = parent != null ? parent.rotation : Quaternion.identity;

                // Convert the _world space_ coords to _local_ coords, relative to parent
                this.rotation = (Quaternion.Inverse(p_rotation) * Quaternion.Euler(rotation)).eulerAngles;
                this.position = Quaternion.Inverse(p_rotation) * position - p_position;
                this.scale = scale;
            }

            CheckState();
        }

        public override object Clone()
        {
            return MemberwiseClone();
        }

        public override (string name, GameObject gameObject) GetUI() 
            => ("Transform", BP.I.WorldEdit.TransformInspector);

        public override void ReplaceValues(WOCBase wOCBase)
        {
            WOCTransform t = wOCBase as WOCTransform;
            position = t.position; 
            rotation = t.rotation; 
            scale = t.scale;
        }
    }
}