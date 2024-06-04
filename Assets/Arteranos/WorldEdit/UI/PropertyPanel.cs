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

        public GameObject WorldObject { get; set; }

        protected override void Awake()
        {
            base.Awake();

            chk_Local.onValueChanged.AddListener(_ => SetLocalMode(true));
            chk_Global.onValueChanged.AddListener(_ => SetLocalMode(false));
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            SetLocalMode(true);

            Populate();
        }

        protected override void OnDisable()
        {
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
            Vector3 s = t.localPosition;

            Vector3 r = q.eulerAngles;

            txt_Pos_X.text = p.x.ToString();
            txt_Pos_Y.text = p.y.ToString();
            txt_Pos_Z.text = p.z.ToString();

            txt_Rot_X.text = r.x.ToString();
            txt_Rot_Y.text = r.y.ToString();
            txt_Rot_Z.text = r.z.ToString();

            txt_Scale_X.text = s.x.ToString();
            txt_Scale_Y.text = s.y.ToString();
            txt_Scale_Z.text = s.z.ToString();

            Color col;
            if (t.TryGetComponent(out Renderer renderer))
                col = renderer.material.color;
            else
                col = Color.white;

            txt_Col_R.text = col.r.ToString();
            txt_Col_G.text = col.g.ToString();
            txt_Col_B.text = col.b.ToString();

            // TODO Adjust hue slider and color gradient square
            img_Color_Swatch.color = col;

            lbl_Heading.text = WorldObject.name;
        }

        private void SetLocalMode(bool local)
        {
            chk_Local.SetIsOnWithoutNotify(local);
            chk_Global.SetIsOnWithoutNotify(!local);

            Populate();
        }
    }
}
