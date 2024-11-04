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
    public class TeleportButtonInspector : UIBehaviour, IInspector
    {
        // TODO QoL improvements, like spinner for same-world location selection.
        // RFU Spinner to browse available worlds, converting names <=> CIDs
        public TMP_InputField txt_location;
        public Spinner spn_World;           // RFU
        public Toggle chk_InServer;         // RFU

        public WOCBase Woc
        {
            get => teleportButton;
            set => teleportButton = value as WOCTeleportButton;
        }
        public PropertyPanel PropertyPanel { get; set; }

        private WOCTeleportButton teleportButton;

        protected override void Awake()
        {
            base.Awake();

            Debug.Assert(Woc != null);
            Debug.Assert(PropertyPanel);

            ListPossibleWorlds();

            txt_location.onValueChanged.AddListener(GotLocationChanged);
            spn_World.OnChanged += GetWorldChanged;
            chk_InServer.onValueChanged.AddListener(GotInServerChanged);
        }

        private void ListPossibleWorlds()
        {
            string[] possibleWorlds = new string[]
            {
                "(This world)"
            };

            spn_World.Options = possibleWorlds;
        }

        private void GotInServerChanged(bool arg0)
        {
            teleportButton.inServer = chk_InServer.isOn;
            teleportButton.SetState();
            PropertyPanel.CommitModification(this);
        }

        private void GetWorldChanged(int arg1, bool arg2)
        {
            if(spn_World.value == 0)
                teleportButton.targetWorldCid = null; // In-world teleportation

            teleportButton.SetState();
            PropertyPanel.CommitModification(this);
        }

        private void GotLocationChanged(string arg0)
        {
            teleportButton.location = txt_location.text;
            teleportButton.SetState();
            PropertyPanel.CommitModification(this);
        }

        public void Populate()
        {
            ListPossibleWorlds();

            txt_location.text = teleportButton.location;
            spn_World.value = 0; // RFU
            chk_InServer.isOn = teleportButton.inServer;
        }
    }
}