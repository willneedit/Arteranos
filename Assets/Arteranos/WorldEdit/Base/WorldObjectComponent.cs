/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using System.Collections.Generic;
using ProtoBuf;

using Arteranos.Core;
using UnityEngine;
using System.IO;
using System;

namespace Arteranos.WorldEdit
{
    // Just for keeping the data for converting the gameobject to a serializable world object.
    public class WorldObjectComponent : MonoBehaviour
    {
        public WorldObjectAsset Asset { get; set; } = null;
        public bool IsLocked { get; set; } = false;

        public event Action OnStateChanged;

        private Vector3 oldPosition;
        private Quaternion oldRotation;
        private Vector3 oldScale;

        private void Start()
        {
            UpdateOldStates();
        }

        private void Update()
        {
            if (oldPosition != transform.position)
                TriggerStateChanged();
            if (oldRotation != transform.rotation)
                TriggerStateChanged();
            if (oldScale != transform.localScale)
                TriggerStateChanged();
        }

        /// <summary>
        /// Clean the 'dirty' state of the object
        /// </summary>
        public void UpdateOldStates()
        {
            oldPosition = transform.position;
            oldRotation = transform.rotation;
            oldScale = transform.localScale;
        }

        /// <summary>
        /// Notify obserevers about the changed state
        /// </summary>
        public void TriggerStateChanged()
        {
            OnStateChanged?.Invoke();
            UpdateOldStates();
        }
    }
}