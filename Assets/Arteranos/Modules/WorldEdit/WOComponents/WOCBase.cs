﻿/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using ProtoBuf;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arteranos.WorldEdit.Components
{
    /// <summary>
    /// World Object Component - lightweight component system, keeping in sync with
    /// the GameObject components and being serializable and supporting incremental patching.
    /// </summary>

    [ProtoContract]
    [ProtoInclude(65537, typeof(WOCTransform))]
    [ProtoInclude(65538, typeof(WOCColor))]
    [ProtoInclude(65539, typeof(WOCPhysics))]
    [ProtoInclude(65540, typeof(WOCSpawner))]
    public abstract class WOCBase : ICloneable, IHasAssetReferences
    {
        public bool Dirty { get; protected set; } = false;

        public virtual GameObject GameObject { get; set; } = null;

        /// <summary>
        /// To make the changes to take effect.
        /// </summary>
        public virtual void CommitState() => Dirty = false;

        /// <summary>
        /// To read the state of the GameObject and convert the component's data into
        /// the serializable format.
        /// </summary>
        public virtual void CheckState() { }

        public virtual void OnDestroy() { }

        public void Update()
        {
            if (G.WorldEditorData.IsInEditMode)
                CheckState();
        }

        public abstract void ReplaceValues(WOCBase wOCBase);

        public abstract object Clone();

        public abstract (string name, GameObject gameObject) GetUI();

        public virtual HashSet<AssetReference> GetAssetReferences() => new();
    }
}