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
using static UnityEngine.UI.Slider;

namespace Arteranos.UI
{
    [ExecuteAlways]
    public class NumberedSlider : UIBehaviour
    {
        public string Format = "{0:F1}";
        public SliderEvent onValueChanged {
            get => m_OnValueChanged;
            set => m_OnValueChanged = value; 
        }

        [SerializeField]
        private SliderEvent m_OnValueChanged = new();

        private Slider sld_Slider = null;
        private TextMeshProUGUI lbl_number = null;

        protected override void Awake()
        {
            base.Awake();

            sld_Slider = transform.GetChild(0).GetComponent<Slider>();
            lbl_number = transform.GetChild(1).GetComponent<TextMeshProUGUI>();

            sld_Slider.onValueChanged.AddListener(OnInternalValueChanged);
        }

        private void OnInternalValueChanged(float newValue)
        {
            lbl_number.text = string.Format(Format, newValue);
            m_OnValueChanged.Invoke(newValue);
        }

        protected override void Start() => base.Start();

    }
}
