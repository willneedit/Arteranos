﻿/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using ProtoBuf;
using UnityEngine;

namespace Arteranos.WorldEdit
{
    [ProtoContract]
    [ProtoInclude(65537, typeof(WOCTransform))]
    [ProtoInclude(65538, typeof(WOCColor))]
    public abstract class WOCBase
    {
        public bool Dirty { get; protected set; } = false;

        protected GameObject gameObject = null;

        /// <summary>
        /// To make the changes to take effect.
        /// </summary>
        public virtual void CommitState()
        {
            // TODO If dirty, propagate state
            Dirty = false;
        }

        /// <summary>
        /// To read the state of the GameObject and convert the component's data into
        /// the serializable format.
        /// </summary>
        public abstract void CheckState();

        /// <summary>
        /// To stage the changes, but not to commit yet
        /// </summary>
        public void SetState()
        {
            Dirty = true;
        }

        /// <summary>
        /// Set as in default state
        /// </summary>
        public abstract void Init();

        public virtual void Awake(GameObject gameObject)
        {
            this.gameObject = gameObject;
        }

        public void Update()
        {
            CheckState();
        }
    }

    [ProtoContract]
    public class WOCTransform : WOCBase
    {
        [ProtoMember(1)]
        public WOVector3 position;
        [ProtoMember(2)]
        public WOQuaternion rotation;
        [ProtoMember(3)]
        public WOVector3 scale;

        private Transform transform = null;

        public override void Awake(GameObject gameObject)
        {
            base.Awake(gameObject);
            transform = gameObject.transform;
        }

        public override void Init()
        {
            position = Vector3.zero; 
            rotation = Quaternion.identity; 
            scale = Vector3.one;
        }

        public override void CommitState()
        {
            base.CommitState();

            transform.SetLocalPositionAndRotation(position, rotation);
            transform.localScale = scale;
        }

        public override void CheckState()
        {
            if(transform.localPosition != position)
                Dirty = true;
            if(transform.localRotation != rotation)
                Dirty = true;
            if (transform.localScale != scale)
                Dirty = true;
        }

        public void SetState(Vector3 position, Quaternion rotation, Vector3 scale, bool global = false)
        {
            SetState();

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
                this.rotation = Quaternion.Inverse(p_rotation) * rotation;
                this.position = Quaternion.Inverse(p_rotation) * position - p_position;
                this.scale = scale;
            }
        }
    }

    [ProtoContract]
    public class WOCColor : WOCBase
    {
        [ProtoMember(1)]
        public WOColor color;

        private Renderer renderer = null;

        public override void Awake(GameObject gameObject)
        {
            base.Awake(gameObject);
            gameObject.TryGetComponent(out renderer);
        }

        public override void Init()
        {
            color = Color.white;
        }

        public override void CommitState()
        {
            base.CommitState();

            if(renderer != null)
                renderer.material.color = color;
        }

        public override void CheckState()
        {
            if(renderer != null && renderer.material.color != color)
                Dirty = true;
        }

        public void SetState(Color color)
        {
            SetState();

            this.color = color;
        }
    }
}
