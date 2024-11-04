/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.UI;
using Arteranos.WorldEdit.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arteranos.WorldEdit
{
    public class TeleportMarkerInspector : UIBehaviour, IInspector
    {
        public TMP_InputField txt_location;

        public WOCBase Woc 
        { 
            get => teleportMarker; 
            set => teleportMarker = value as WOCTeleportMarker; 
        }
        public PropertyPanel PropertyPanel { get; set; }

        private WOCTeleportMarker teleportMarker;

        protected override void Awake()
        {
            base.Awake();

            Debug.Assert(Woc != null);
            Debug.Assert(PropertyPanel);

            txt_location.onValueChanged.AddListener(GotLocationChanged);
        }

        private void GotLocationChanged(string arg0)
        {
            teleportMarker.location = txt_location.text;
            teleportMarker.SetState();
            PropertyPanel.CommitModification(this);

        }

        public void Populate()
        {
            txt_location.text = teleportMarker.location;
        }
    }
}