/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;
using UnityEngine.EventSystems;

using Arteranos.Core;
using UnityEngine.UI;
using System;

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
        [SerializeField] private Toggle chk_Seconds;

        private Client cs = null;
        private bool dirty = false;

        protected override void Awake()
        {
            base.Awake();

            sldn_AxisX.OnValueChanged += OnAxisXChanged;
            sldn_AxisY.OnValueChanged += OnAxisYChanged;
            sldn_Log2Size.OnValueChanged += OnLog2SizeChanged;
            sldn_Tightness.OnValueChanged += OnTightnessChanged;
            sldn_Delay.OnValueChanged += OnDelayChanged;

            spn_Clock.OnChanged += OnClockChanged;
            chk_Seconds.onValueChanged.AddListener(OnSecondsChanged);
        }

        private void OnAxisXChanged(float obj)
        {
            cs.UserHUD.AxisX = sldn_AxisX.value;
            cs.PingUserHUDChanged();
            dirty = true;
        }

        private void OnAxisYChanged(float obj)
        {
            cs.UserHUD.AxisY = sldn_AxisY.value;
            cs.PingUserHUDChanged();
            dirty = true;
        }

        private void OnLog2SizeChanged(float obj)
        {
            cs.UserHUD.Log2Size = sldn_Log2Size.value;
            cs.PingUserHUDChanged();
            dirty = true;
        }

        private void OnTightnessChanged(float obj)
        {
            cs.UserHUD.Tightness = sldn_Tightness.value;
            cs.PingUserHUDChanged();
            dirty = true;
        }

        private void OnDelayChanged(float obj)
        {
            cs.UserHUD.Delay = sldn_Delay.value;
            cs.PingUserHUDChanged();
            dirty = true;
        }

        private void OnClockChanged(int arg1, bool arg2)
        {
            cs.UserHUD.ClockDisplay = spn_Clock.value;
            chk_Seconds.interactable = cs.UserHUD.ClockDisplay != 0;
            cs.PingUserHUDChanged();
            dirty = true;
        }

        private void OnSecondsChanged(bool arg0)
        {
            cs.UserHUD.Seconds = chk_Seconds.isOn;
            cs.PingUserHUDChanged();
            dirty = true;
        }

        protected override void Start()
        {
            base.Start();

            cs = SettingsManager.Client;

            sldn_AxisX.value = cs.UserHUD.AxisX;
            sldn_AxisY.value = cs.UserHUD.AxisY;
            sldn_Log2Size.value = cs.UserHUD.Log2Size;
            sldn_Tightness.value = cs.UserHUD.Tightness;
            sldn_Delay.value = cs.UserHUD.Delay;

            spn_Clock.value = cs.UserHUD.ClockDisplay;
            chk_Seconds.isOn = cs.UserHUD.Seconds;
            chk_Seconds.interactable = cs.UserHUD.ClockDisplay != 0;

            // Reset the state as it's the initial state, not the blank slate.
            dirty = false;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            G.SysMenu.ShowUserHUD(true);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            G.SysMenu.ShowUserHUD(false);

            // Might be to disabled before it's really started, so cs may be null yet.
            if(dirty) cs?.Save();
            dirty = false;
            cs?.PingUserHUDChanged();
        }
    }
}
