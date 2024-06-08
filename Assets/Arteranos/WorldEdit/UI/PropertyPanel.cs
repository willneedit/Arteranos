/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;
using Arteranos.UI;

namespace Arteranos.WorldEdit
{
    public class PropertyPanel : UIBehaviour
    {
        public Button btn_ReturnToList;
        public TextMeshProUGUI lbl_Heading;
        public TMP_InputField txt_Pos_X;
        public TMP_InputField txt_Pos_Y;
        public TMP_InputField txt_Pos_Z;

        public TMP_InputField txt_Rot_X;
        public TMP_InputField txt_Rot_Y;
        public TMP_InputField txt_Rot_Z;

        public TMP_InputField txt_Scale_X;
        public TMP_InputField txt_Scale_Y;
        public TMP_InputField txt_Scale_Z;

        public TMP_InputField txt_Col_R;
        public TMP_InputField txt_Col_G;
        public TMP_InputField txt_Col_B;

        public RawImage img_Color_Swatch;

        public Toggle chk_Local;
        public Toggle chk_Global;
        public Slider sld_Hue;

        public GameObject WorldObject
        {
            get => m_WorldObject;
            set
            {
                m_WorldObject = value;
                Woc = m_WorldObject.GetComponent<WorldObjectComponent>();
            }
        }

        public WorldObjectComponent Woc { get; private set; }

        private GameObject m_WorldObject;
        private ObjectChooser Chooser = null;

        protected override void Awake()
        {
            base.Awake();

            btn_ReturnToList.onClick.AddListener(OnReturnToChooserClicked);
            chk_Local.onValueChanged.AddListener(_ => SetLocalMode(true));
            chk_Global.onValueChanged.AddListener(_ => SetLocalMode(false));

            txt_Pos_X.onValueChanged.AddListener(CommitChangedValues);
            txt_Pos_Y.onValueChanged.AddListener(CommitChangedValues);
            txt_Pos_Z.onValueChanged.AddListener(CommitChangedValues);

            txt_Rot_X.onValueChanged.AddListener(CommitChangedValues);
            txt_Rot_Y.onValueChanged.AddListener(CommitChangedValues);
            txt_Rot_Z.onValueChanged.AddListener(CommitChangedValues);

            txt_Scale_X.onValueChanged.AddListener(CommitChangedValues);
            txt_Scale_Y.onValueChanged.AddListener(CommitChangedValues);
            txt_Scale_Z.onValueChanged.AddListener(CommitChangedValues);

            txt_Col_R.onValueChanged.AddListener(CommitChangedValues);
            txt_Col_G.onValueChanged.AddListener(CommitChangedValues);
            txt_Col_B.onValueChanged.AddListener(CommitChangedValues);
        }

        protected override void Start()
        {
            base.Start();

            Chooser = transform.parent.GetComponentInChildren<ObjectChooser>(true);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            SetLocalMode(true);

            Populate();

            Woc.OnStateChanged += Populate;
        }

        protected override void OnDisable()
        {
            Woc.OnStateChanged -= Populate;

            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        private void Populate()
        {
            Transform t = WorldObject.transform;
            Vector3 p = chk_Local.isOn ? t.localPosition : t.position;
            Quaternion q = chk_Local.isOn ? t.localRotation : t.rotation;
            Vector3 s = t.localScale;

            Vector3 r = q.eulerAngles;

            txt_Pos_X.SetTextWithoutNotify(p.x.ToStringInvariant("N"));
            txt_Pos_Y.SetTextWithoutNotify(p.y.ToStringInvariant("N"));
            txt_Pos_Z.SetTextWithoutNotify(p.z.ToStringInvariant("N"));

            txt_Rot_X.SetTextWithoutNotify(r.x.ToStringInvariant("N"));
            txt_Rot_Y.SetTextWithoutNotify(r.y.ToStringInvariant("N"));
            txt_Rot_Z.SetTextWithoutNotify(r.z.ToStringInvariant("N"));

            txt_Scale_X.SetTextWithoutNotify(s.x.ToStringInvariant("N"));
            txt_Scale_Y.SetTextWithoutNotify(s.y.ToStringInvariant("N"));
            txt_Scale_Z.SetTextWithoutNotify(s.z.ToStringInvariant("N"));

            Color col;
            if (t.TryGetComponent(out Renderer renderer))
                col = renderer.material.color;
            else
                col = Color.white;

            txt_Col_R.SetTextWithoutNotify(col.r.ToStringInvariant("N"));
            txt_Col_G.SetTextWithoutNotify(col.g.ToStringInvariant("N"));
            txt_Col_B.SetTextWithoutNotify(col.b.ToStringInvariant("N"));

            // TODO Adjust hue slider and color gradient square
            img_Color_Swatch.color = col;

            lbl_Heading.text = WorldObject.name;
        }

        private void CommitChangedValues(string arg0)
        {
            Vector3 p = new(
                txt_Pos_X.text.ParseInvariant(),
                txt_Pos_Y.text.ParseInvariant(),
                txt_Pos_Z.text.ParseInvariant());
            Quaternion r = Quaternion.Euler(
                txt_Rot_X.text.ParseInvariant(),
                txt_Rot_Y.text.ParseInvariant(),
                txt_Rot_Z.text.ParseInvariant());
            Vector3 s = new(
                txt_Scale_X.text.ParseInvariant(),
                txt_Scale_Y.text.ParseInvariant(),
                txt_Scale_Z.text.ParseInvariant());

            Transform t = WorldObject.transform;

            if (chk_Local.isOn)
                t.SetLocalPositionAndRotation(p, r);
            else
                t.SetPositionAndRotation(p, r);
            t.localScale = s;

            if (t.TryGetComponent(out Renderer renderer))
                renderer.material.color = new Color(
                    txt_Col_R.text.ParseInvariant(),
                    txt_Col_G.text.ParseInvariant(),
                    txt_Col_B.text.ParseInvariant());

            // Prevent the loopback of the updated world object
            Woc.UpdateOldStates();
        }

        private void SetLocalMode(bool local)
        {
            chk_Local.SetIsOnWithoutNotify(local);
            chk_Global.SetIsOnWithoutNotify(!local);

            Populate();
        }

        private void OnReturnToChooserClicked()
        {
            gameObject.SetActive(false);
            Chooser.gameObject.SetActive(true);
        }

#if UNITY_EDITOR
        public void Test_OnReturnToChooserClicked() => OnReturnToChooserClicked();
        public void Test_SetLocalMode(bool local) => SetLocalMode(local);
#endif
    }

    // Culture variant conversions by default is BAD - Broken As Designed.
    // Seriously. I've seen it as a bug in a product from Microsoft itself --- Altspace!
    public static class ConversionExtension
    {
        private static readonly IFormatProvider inv
                       = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;

        public static string ToStringInvariant<T>(this T obj, string format = null)
        {
            return (format == null) ? FormattableString.Invariant($"{obj}")
                                    : String.Format(inv, $"{{0:{format}}}", obj);
        }

        public static float ParseInvariant(this string str)
        {
            return float.Parse(str, 
                System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
        }
    }
}
