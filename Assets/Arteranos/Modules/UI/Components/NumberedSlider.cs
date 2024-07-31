/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arteranos.UI
{
    [ExecuteAlways]
    public class NumberedSlider : UIBehaviour
    {
        public string Format = "{0:F1}";

        public event Action<float> OnValueChanged = null;

        public float value {
            get => sld_Slider.value;
            set
            {
                sld_Slider.value = value;
                lbl_number.text = string.Format(Format, value);
            }
        }

        public bool interactable 
        {
            get => sld_Slider.interactable;
            set => sld_Slider.interactable = value;
        }

        private Slider sld_Slider => transform.GetChild(0).GetComponent<Slider>();
        private TextMeshProUGUI lbl_number => transform.GetChild(1).GetComponent<TextMeshProUGUI>();

        protected override void Awake()
        {
            base.Awake();

            sld_Slider.onValueChanged.AddListener(OnInternalValueChanged);
        }

        private void OnInternalValueChanged(float newValue)
        {
            lbl_number.text = string.Format(Format, newValue);
            OnValueChanged?.Invoke(newValue);
        }

        protected override void Start() => base.Start();

    }
}
