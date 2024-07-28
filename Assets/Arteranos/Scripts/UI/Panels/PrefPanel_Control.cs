/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Arteranos.Core;
using System.Collections.Generic;
using System.Linq;

namespace Arteranos.UI
{
    public class PrefPanel_Control : UIBehaviour
    {
        [SerializeField] private Spinner spn_vk_active = null;
        [SerializeField] private Spinner spn_vk_layout = null;

        [SerializeField] private NumberedSlider sldn_NameplateIn = null;
        [SerializeField] private NumberedSlider sldn_NameplateOut = null;

        [SerializeField] private Toggle chk_ctrl_left = null;
        [SerializeField] private Toggle chk_ctrl_right = null;
        [SerializeField] private Toggle chk_active_left = null;
        [SerializeField] private Toggle chk_active_right = null;
        [SerializeField] private Spinner spn_stickuse_left = null;
        [SerializeField] private Spinner spn_stickuse_right = null;
        [SerializeField] private Spinner spn_type_left = null;
        [SerializeField] private Spinner spn_type_right = null;

        [SerializeField] private GameObject grp_Ray_Controls = null;

        private Client cs = null;
        private ControlSettingsJSON controls;
        private bool dirty = false;

        private Dictionary<string, VKUsage> spne_VKUsage;
        private Dictionary<string, VKLayout> spne_VKLayout;
        private Dictionary<string, RayType> spne_raytype;

        // FIXME / HACK: Adapt analogous to PrefPanel_Movement because of
        // potential side effects to changed settings.
        protected override void Awake()
        {
            base.Awake();

            spn_vk_active.OnChanged += OnVKActiveChanged;
            spn_vk_layout.OnChanged += OnVKLayoutChanged;

            sldn_NameplateIn.OnValueChanged += OnNameplaneInChanged;
            sldn_NameplateOut.OnValueChanged += OnNameplaneOutChanged;

            chk_ctrl_left.onValueChanged.AddListener(OnLeftControllerEnabled);
            chk_ctrl_right.onValueChanged.AddListener(OnRightControllerEnabled);
            chk_active_left.onValueChanged.AddListener(OnLeftControllerAlwaysSeen);
            chk_active_right.onValueChanged.AddListener(OnRightControllerAlwaysSeen);

            spn_stickuse_left.OnChanged += Spn_stickuse_left_OnChanged;
            spn_stickuse_right.OnChanged += Spn_stickuse_right_OnChanged;
            spn_type_left.OnChanged += OnLeftControllerTypeChanged;
            spn_type_right.OnChanged += OnRightControllerTypeChanged;
        }

        private void OnNameplaneInChanged(float obj)
        {
            controls.NameplateIn = sldn_NameplateIn.value;
            OnControllersChanged();
        }

        private void OnNameplaneOutChanged(float obj)
        {
            controls.NameplateOut = sldn_NameplateOut.value;
            OnControllersChanged();
        }

        private void OnVKActiveChanged(int arg1, bool arg2)
        {
            controls.VK_Usage = spn_vk_active.GetEnumValue(spne_VKUsage);
            OnControllersChanged();
        }

        private void OnVKLayoutChanged(int arg1, bool arg2)
        {
            controls.VK_Layout = spn_vk_layout.GetEnumValue(spne_VKLayout);
            OnControllersChanged();
        }

        private void OnLeftControllerEnabled(bool arg0)
        {
            controls.Controller_left = chk_ctrl_left.isOn;
            OnControllersChanged();
        }

        private void OnRightControllerEnabled(bool arg0)
        {
            controls.Controller_right = chk_ctrl_right.isOn;
            OnControllersChanged();
        }

        private void OnLeftControllerAlwaysSeen(bool arg0)
        {
            controls.Controller_active_left = chk_active_left.isOn;
            OnControllersChanged();
        }

        private void OnRightControllerAlwaysSeen(bool arg0)
        {
            controls.Controller_active_right = chk_active_right.isOn;
            OnControllersChanged();
        }

        private void OnLeftControllerTypeChanged(int arg1, bool arg2)
        {
            controls.Controller_Type_left = spn_type_left.GetEnumValue(spne_raytype);
            OnControllersChanged();
        }

        private void OnRightControllerTypeChanged(int arg1, bool arg2)
        {
            controls.Controller_Type_right = spn_type_right.GetEnumValue(spne_raytype);
            OnControllersChanged();
        }

        private void Spn_stickuse_left_OnChanged(int arg1, bool arg2)
        {
            controls.StickType_Left = (StickType)arg1;
            OnControllersChanged();
        }

        private void Spn_stickuse_right_OnChanged(int arg1, bool arg2)
        {
            controls.StickType_Right = (StickType)arg1;
            OnControllersChanged();
        }


        private void OnControllersChanged()
        {
            // Keep at least one controller on if you're in VR.
            if (!chk_ctrl_left.isOn && !chk_ctrl_right.isOn && cs.VRMode)
                chk_ctrl_right.isOn = true;

            cs.PingXRControllersChanged();
            dirty = true;
        }

        protected override void Start()
        {
            base.Start();

            cs = G.Client;

            controls = cs.Controls;

            spn_vk_active.FillSpinnerEnum(out spne_VKUsage, controls.VK_Usage);
            spn_vk_layout.FillSpinnerEnum(out spne_VKLayout, controls.VK_Layout);

            UIUtils.CreateEnumValues(out spne_raytype);
            spn_type_left.Options = spne_raytype.Keys.ToArray();
            spn_type_right.Options = spne_raytype.Keys.ToArray();

            sldn_NameplateIn.value = controls.NameplateIn;
            sldn_NameplateOut.value = controls.NameplateOut;

            grp_Ray_Controls.SetActive(cs.VRMode);

            chk_ctrl_left.isOn = controls.Controller_left;
            chk_ctrl_right.isOn = controls.Controller_right;

            chk_active_left.isOn = controls.Controller_active_left;
            chk_active_right.isOn = controls.Controller_active_right;

            spn_stickuse_left.value = (int) controls.StickType_Left;
            spn_stickuse_right.value = (int) controls.StickType_Right;

            spn_type_left.value = (int) controls.Controller_Type_left;
            spn_type_right.value = (int) controls.Controller_Type_right;

            // Reset the state as it's the initial state, not the blank slate.
            dirty = false;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            // Might be to disabled before it's really started, so cs may be null yet.
            if (dirty) cs?.Save();
            dirty = false;
            cs?.PingXRControllersChanged();
        }
    }
}
