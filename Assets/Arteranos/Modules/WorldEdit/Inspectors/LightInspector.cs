/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using Arteranos.UI;
using Arteranos.WorldEdit.Components;
using Ipfs.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;
using TaskScheduler = Arteranos.Core.TaskScheduler;

namespace Arteranos.WorldEdit
{
    public class LightInspector : UIBehaviour, IInspector
    {
#pragma warning disable IDE0044 // Modifizierer "readonly" hinzufügen
        [SerializeField] private ColorPicker col_Color;
        [SerializeField] private Spinner spn_Type;
        [SerializeField] private NumberedSlider sld_IntensityLog;
        [SerializeField] private NumberedSlider sld_Range;
        [SerializeField] private NumberedSlider sld_Angle;
#pragma warning restore IDE0044 // Modifizierer "readonly" hinzufügen

        struct LightTypeEntry
        {
            public string name;
            public LightType type;

            public LightTypeEntry(string name, LightType type)
            {
                this.name = name;
                this.type = type;
            }
        }
        private static readonly List<LightTypeEntry> _lightTypes = new()
        {
            new("Directional", LightType.Directional),
            new("Point", LightType.Point ),
            new("Spot", LightType.Spot ),
        };

        private static Dictionary<LightType, int> _lightTypeIndices = null;

        public WOCBase Woc
        {
            get => _light;
            set => _light = value as WOCLight;
        }

        public PropertyPanel PropertyPanel { get; set; }

        private WOCLight _light;

        protected override void Awake()
        {
            base.Awake();

            Debug.Assert(Woc != null);
            Debug.Assert(PropertyPanel);

            if(_lightTypeIndices == null)
            {
                _lightTypeIndices = new();
                for(int i = 0; i < _lightTypes.Count; i++)
                    _lightTypeIndices[_lightTypes[i].type] = i;
            }

            spn_Type.Options = (from entry in _lightTypes select entry.name).ToArray();
            col_Color.OnColorChanged += _ => GotValueChanged();
            spn_Type.OnChanged += (i, b) => GotValueChanged();
            sld_IntensityLog.OnValueChanged += _ => GotValueChanged();
            sld_Range.OnValueChanged += _ => GotValueChanged();
            sld_Angle.OnValueChanged += _ => GotValueChanged();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Populate();
        }

        public void Populate()
        {
            col_Color.SetColorWithoutNotify(_light.color);
            spn_Type.value = _lightTypeIndices[_light.type];
            sld_IntensityLog.value = Mathf.Log10(_light.intensity);
            sld_Range.value = _light.range;
            sld_Angle.value = _light.angle;
        }

        private void GotValueChanged()
        {
            _light.color = col_Color.Color;
            _light.type = _lightTypes[spn_Type.value].type;
            _light.intensity = Mathf.Pow(10, sld_IntensityLog.value);
            _light.range = sld_Range.value;
            _light.angle = sld_Angle.value;

            _light.SetState();

            PropertyPanel.CommitModification(this);
        }
    }
}