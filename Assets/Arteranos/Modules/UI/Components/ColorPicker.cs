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

        public RawImage img_Old;
        public RawImage img_New;
        public TMP_InputField txt_Hex;

        public Color Color
        {
            get => color;
            set
            {
                Color old = color;
                if(value != old)
                {
                    color = value;
                    img_New.color = color;
                    txt_Hex.SetTextWithoutNotify(string.Format("{0:X2}{1:X2}{2:X2}", (int)(color.r * 255), (int)(color.g * 255), (int)(color.b * 255)));
                    OnColorChanged?.Invoke(color);
                }
            }
        }

        public event Action<Color> OnColorChanged;

        private Color color = Color.white;

        private RawImage svPanelImage = null;

        private Vector3 HSV = Vector3.zero;

        protected override void Awake()
        {
            base.Awake();

            btn_HuePanel.OnClick += GotHuePanelClick;
            btn_SVPanel.OnClick += GotSVPanelClick;

            txt_H.onValueChanged.AddListener(s => GotHSVEntered(s, 0));
            txt_S.onValueChanged.AddListener(s => GotHSVEntered(s, 1));
            txt_V.onValueChanged.AddListener(s => GotHSVEntered(s, 2));

            txt_R.onValueChanged.AddListener(s => GotRGBEntered(s, 0));
            txt_G.onValueChanged.AddListener(s => GotRGBEntered(s, 1));
            txt_B.onValueChanged.AddListener(s => GotRGBEntered(s, 2));

            txt_Hex.onValueChanged.AddListener(GotHexEntered);

            btn_SVPanel.TryGetComponent(out svPanelImage);

            // Copy to save blueprint;
            svPanelImage.material = new(svPanelImage.material);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            img_Old.color = Color;
            
            HSV = RGBtoHSV(Color);
            UpdateHuePanel();
            UpdateHSV();
            UpdateRGB();
        }


        private void UpdateHuePanel()
        {
            svPanelImage.material.color = HSVtoRGB(new Vector3(HSV.x, 1, 1));
        }

        private void GotRGBEntered(string s, int which)
        {
            float value = Get01Input(s);

            Color newcol = Color;
            switch (which)
            {
                case 0: newcol.r = value; break;
                case 1: newcol.g = value; break;
                case 2: newcol.b = value; break;
            }

            Color = newcol;

            HSV = RGBtoHSV(Color);

            UpdateHuePanel();
            UpdateHSV();
        }

        private void GotHSVEntered(string s, int which)
        {
            float value = Get01Input(s);

            switch (which)
            {
                case 0:
                    HSV.x = value;
                    UpdateHuePanel();
                    break;
                case 1: HSV.y = value; break;
                case 2: HSV.z = value; break;
            }

            Color = HSVtoRGB(HSV);

            UpdateRGB();
        }

        private static float Get01Input(string s)
        {
            float value = 0;
            try
            {
                value = float.Parse(s);
            }
            catch { }

            // No. Glowjobs. PERIOD!
            value = Mathf.Clamp01(value);
            return value;
        }

        private void GotHexEntered(string s)
        {
            if (s.Length != 6) return;

            Color newcol = Color;
            newcol.r = GetHexByte(s[0..2]) / 255.0f;
            newcol.g = GetHexByte(s[2..4]) / 255.0f;
            newcol.b = GetHexByte(s[4..6]) / 255.0f;

            Color = newcol;

            HSV = RGBtoHSV(Color);
            UpdateHuePanel();
            UpdateHSV();
            UpdateRGB();
        }

        private int GetHexByte(string s)
        {
            try
            {
                return int.Parse(s, System.Globalization.NumberStyles.HexNumber);
            }
            catch (FormatException) { return 0; }
        }

        private void GotSVPanelClick(Vector2 vector)
        {
            HSV.y = vector.x;
            HSV.z = vector.y;

            txt_S.text = HSV.y.ToString("F4");
            txt_V.text = HSV.z.ToString("F4");

            Color = HSVtoRGB(HSV);
            UpdateRGB();
        }

        private void GotHuePanelClick(Vector2 vector)
        {
            HSV.x = 1.0f - vector.y; // The start of the rainbow is on the top, not the bottom.
            txt_H.text = HSV.x.ToString("F4");
            UpdateHuePanel();

            Color = HSVtoRGB(HSV);
            UpdateRGB();
        }

        private void UpdateRGB()
        {
            txt_R.SetTextWithoutNotify(Color.r.ToString("F4"));
            txt_G.SetTextWithoutNotify(Color.g.ToString("F4"));
            txt_B.SetTextWithoutNotify(Color.b.ToString("F4"));
        }

        private void UpdateHSV()
        {
            txt_H.SetTextWithoutNotify(HSV.x.ToString("F4"));
            txt_S.SetTextWithoutNotify(HSV.y.ToString("F4"));
            txt_V.SetTextWithoutNotify(HSV.z.ToString("F4"));
        }


        // Gleaned from shader programs....
        private static Color HSVtoRGB(Vector3 hsv)
        {
            static float Frac(float value) => value - Mathf.Floor(value);

            Vector4 K = new(1.0f, 2.0f / 3.0f, 1.0f / 3.0f, 3.0f);
            Vector3 P = new(
                Mathf.Abs(Frac(hsv.x + K.x) * 6.0f - K.w),
                Mathf.Abs(Frac(hsv.x + K.y) * 6.0f - K.w),
                Mathf.Abs(Frac(hsv.x + K.z) * 6.0f - K.w)
                );

            return new Color(
                Mathf.Lerp(K.x, Mathf.Clamp01(P.x - K.x), hsv.y) * hsv.z,
                Mathf.Lerp(K.x, Mathf.Clamp01(P.y - K.x), hsv.y) * hsv.z,
                Mathf.Lerp(K.x, Mathf.Clamp01(P.z - K.x), hsv.y) * hsv.z
                );
        }

        private static Vector3 RGBtoHSV(Color In)
        {
            static float step(float y, float x) => (x >= y) ? 1 : 0;

            Vector4 K = new(0.0f, -1.0f / 3.0f, 2.0f / 3.0f, -1.0f);
            Vector4 P = Vector4.Lerp(new Vector4(In.b, In.g, K.w, K.z), new Vector4(In.g, In.b, K.x, K.y), step(In.b, In.g));
            Vector4 Q = Vector4.Lerp(new Vector4(P.x, P.y, P.w, In.r), new Vector4(In.r, P.y, P.z, P.x), step(P.x, In.r));
            float D = Q.x - Mathf.Min(Q.w, Q.y);
            float E = float.Epsilon;
            return new Vector3(
                Mathf.Abs(Q.z + (Q.w - Q.y) / (6.0f * D + E)), 
                D / (Q.x + E), 
                Q.x);
        }
    }
}
