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
    public class PrefPanel_UserHUD : UIBehaviour
    {
        [SerializeField] private NumberedSlider sldn_AxisX;
        [SerializeField] private NumberedSlider sldn_AxisY;
        [SerializeField] private NumberedSlider sldn_Log2Size;

        [SerializeField] private NumberedSlider sldn_Tightness;
        [SerializeField] private NumberedSlider sldn_Delay;

        [SerializeField] private Spinner spn_Clock;

        private ClientSettings cs = null;
        private bool dirty = false;

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();

            cs = SettingsManager.Client;

            // Reset the state as it's the initial state, not the blank slate.
            dirty = false;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            // Might be to disabled before it's really started, so cs may be null yet.
            if(dirty) cs?.SaveSettings();
            dirty = false;
        }
    }
}
