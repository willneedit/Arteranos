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
        [SerializeField] private Spinner spn_type_left = null;
        [SerializeField] private Spinner spn_type_right = null;

        private ClientSettings cs = null;

        private Dictionary<string, VKUsage> spne_VKUsage;
        private Dictionary<string, VKLayout> spne_VKLayout;
        private Dictionary<string, RayType> spne_raytype;

        // FIXME / HACK: Adapt analogous to PrefPanel_Movement because of
        // potential side effects to changed settings.
        protected override void Awake()
        {
            base.Awake();

            chk_ctrl_left.onValueChanged.AddListener(OnControllersChanged);
            chk_ctrl_right.onValueChanged.AddListener(OnControllersChanged);
            chk_active_left.onValueChanged.AddListener(OnControllersChanged);
            chk_active_right.onValueChanged.AddListener(OnControllersChanged);

            spn_type_left.OnChanged += OnControllerChanged2;
            spn_type_right.OnChanged += OnControllerChanged2;
        }

        private void OnControllersChanged(bool arg0)
        {
            UploadSettings();
            cs.PingXRControllersChanged();
        }

        private void OnControllerChanged2(int arg1, bool arg2)
        {
            UploadSettings();
            cs.PingXRControllersChanged();
        }

        protected override void Start()
        {
            base.Start();

            cs = SettingsManager.Client;

            ControlSettingsJSON controls = cs.Controls;

            spn_vk_active.FillSpinnerEnum(out spne_VKUsage, controls.VK_Usage);
            spn_vk_layout.FillSpinnerEnum(out spne_VKLayout, controls.VK_Layout);

            sldn_NameplateIn.value = controls.NameplateIn;
            sldn_NameplateOut.value = controls.NameplateOut;

            bool both = cs.VRMode;

            chk_ctrl_left.interactable = both;
            chk_active_left.interactable = both;
            spn_type_left.enabled= both;

            chk_ctrl_left.isOn = controls.controller_left;
            chk_ctrl_right.isOn = controls.controller_right;

            chk_active_left.isOn = controls.controller_active_left;
            chk_active_right.isOn = controls.controller_active_right;

            UIUtils.CreateEnumValues(out spne_raytype);
            spn_type_left.Options = spne_raytype.Keys.ToArray();
            spn_type_right.Options = spne_raytype.Keys.ToArray();
            spn_type_left.SetEnumValue(controls.controller_Type_left);
            spn_type_right.SetEnumValue(controls.controller_Type_right);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            UploadSettings();

            // Might be to disabled before it's really started, so cs may be null yet.
            cs?.SaveSettings();
        }

        private void UploadSettings()
        {
            ControlSettingsJSON controls = cs?.Controls;

            if(controls != null)
            {
                if((!chk_ctrl_left.isOn || !cs.VRMode) && !chk_ctrl_right.isOn)
                    chk_ctrl_right.isOn = true;

                controls.VK_Usage = spn_vk_active.GetEnumValue(spne_VKUsage);
                controls.VK_Layout = spn_vk_layout.GetEnumValue(spne_VKLayout);

                controls.NameplateIn = sldn_NameplateIn.value;
                controls.NameplateOut = sldn_NameplateOut.value;

                controls.controller_left = chk_ctrl_left.isOn;
                controls.controller_right = chk_ctrl_right.isOn;

                controls.controller_active_left = chk_active_left.isOn;
                controls.controller_active_right = chk_active_right.isOn;

                controls.controller_Type_left = spn_type_left.GetEnumValue(spne_raytype);
                controls.controller_Type_right = spn_type_right.GetEnumValue(spne_raytype);
            }
        }
    }
}