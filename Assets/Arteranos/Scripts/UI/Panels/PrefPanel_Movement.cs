/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Arteranos.Core;

namespace Arteranos.UI
{
    public class PrefPanel_Movement : UIBehaviour
    {
        [SerializeField] private Toggle chk_Flying = null;
        [SerializeField] private Spinner spn_Turning= null;
        [SerializeField] private Spinner spn_Teleporting= null;
        [SerializeField] private Spinner spn_Blinders = null;

        private ClientSettings cs = null;

        private Dictionary<string, TurnType> spne_turn;
        private Dictionary<string, TeleportType> spne_teleport;
        private Dictionary<string, ComfortBlindersType> spne_comfortblinders;

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();

            cs = SettingsManager.Client;

            MovementSettingsJSON movement = cs.Movement;

            chk_Flying.isOn = movement.Flying;
            spn_Turning.FillSpinnerEnum(out spne_turn, movement.Turn);
            spn_Teleporting.FillSpinnerEnum(out spne_teleport, movement.Teleport);
            spn_Blinders.FillSpinnerEnum(out spne_comfortblinders, movement.ComfortBlinders);

        }

        protected override void OnDisable()
        {
            base.OnDisable();

            MovementSettingsJSON movement = cs?.Movement;

            if(movement == null) return;

            movement.Flying = chk_Flying.isOn;
            movement.Turn = spn_Turning.GetEnumValue(spne_turn);
            movement.Teleport = spn_Teleporting.GetEnumValue(spne_teleport);
            movement.ComfortBlinders = spn_Blinders.GetEnumValue(spne_comfortblinders);

            // Might be to disabled before it's really started, so cs may be null yet.
            cs?.SaveSettings();
        }
    }
}
