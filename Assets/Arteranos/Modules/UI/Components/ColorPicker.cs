/*
 * Copyright (c) 2024, willneedit
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

namespace Arteranos
{
    public class ColorPicker : UIBehaviour
    {
        public MapButton btn_SVPanel;
        public MapButton btn_HuePanel;

        public TMP_InputField txt_R;
        public TMP_InputField txt_G;
        public TMP_InputField txt_B;

        public TMP_InputField txt_H;
        public TMP_InputField txt_S;
        public TMP_InputField txt_V;

        public Color Color { get => color; set => color = value; }

        private Color color;

        private RawImage svPanelImage = null;

        protected override void Awake()
        {
            base.Awake();

            btn_HuePanel.OnClick += GotHuePanelClick;
            btn_SVPanel.OnClick += GotSVPanelClick;

            btn_SVPanel.TryGetComponent(out svPanelImage);

            // Copy to save blueprint;
            svPanelImage.material = new(svPanelImage.material);
        }

        protected override void Start()
        {
            base.Start();

            GotHuePanelClick(Vector2.zero);
        }

        private void GotSVPanelClick(Vector2 vector)
        {
            throw new NotImplementedException();
        }

        private void GotHuePanelClick(Vector2 vector)
        {
            float Frac(float value) { return value - MathF.Truncate(value); }
            float hue = (1 - vector.y);

            Vector4 K = new(1.0f, 2.0f / 3.0f, 1.0f / 3.0f, 3.0f);
            Vector3 P = new(
                MathF.Abs((float)(Frac(hue + K.x) * 6.0 - K.w)),
                MathF.Abs((float)(Frac(hue + K.y) * 6.0 - K.w)),
                MathF.Abs((float)(Frac(hue + K.z) * 6.0 - K.w))
                );

            Color Out = new(
                Mathf.Clamp01(P.x - K.x),
                Mathf.Clamp01(P.y - K.x),
                Mathf.Clamp01(P.z - K.x)
                );
            txt_R.text = Out.r.ToString();
            txt_G.text = Out.g.ToString();
            txt_B.text = Out.b.ToString();

            txt_H.text = hue.ToString();

            svPanelImage.material.color = Out;
        }
    }
}
