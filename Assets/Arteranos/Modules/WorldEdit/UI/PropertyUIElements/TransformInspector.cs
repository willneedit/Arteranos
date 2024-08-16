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

namespace Arteranos.WorldEdit
{
    public class TransformInspector : UIBehaviour, IInspector
    {
        public TMP_InputField txt_Pos_X;
        public TMP_InputField txt_Pos_Y;
        public TMP_InputField txt_Pos_Z;

        public TMP_InputField txt_Rot_X;
        public TMP_InputField txt_Rot_Y;
        public TMP_InputField txt_Rot_Z;

        public TMP_InputField txt_Scale_X;
        public TMP_InputField txt_Scale_Y;
        public TMP_InputField txt_Scale_Z;

        public WOCBase Woc { get; set; }
        public PropertyPanel PropertyPanel { get; set; }

        protected override void Awake()
        {
            base.Awake();

            Debug.Assert(Woc != null);
            Debug.Assert(PropertyPanel);

            txt_Pos_X.onValueChanged.AddListener(GotValuesChanged);
            txt_Pos_Y.onValueChanged.AddListener(GotValuesChanged);
            txt_Pos_Z.onValueChanged.AddListener(GotValuesChanged);

            txt_Rot_X.onValueChanged.AddListener(GotValuesChanged);
            txt_Rot_Y.onValueChanged.AddListener(GotValuesChanged);
            txt_Rot_Z.onValueChanged.AddListener(GotValuesChanged);

            txt_Scale_X.onValueChanged.AddListener(GotValuesChanged);
            txt_Scale_Y.onValueChanged.AddListener(GotValuesChanged);
            txt_Scale_Z.onValueChanged.AddListener(GotValuesChanged);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Populate();
        }

        public void Populate()
        {
            Transform t = Woc.GameObject.transform;
            Vector3 p = G.WorldEditorData.UsingGlobal ? t.position : t.localPosition;
            Quaternion q = G.WorldEditorData.UsingGlobal ? t.rotation : t.localRotation;
            Vector3 s = t.localScale;

            Vector3 r = q.eulerAngles;

            txt_Pos_X.SetTextWithoutNotify(p.x.ToString("F4"));
            txt_Pos_Y.SetTextWithoutNotify(p.y.ToString("F4"));
            txt_Pos_Z.SetTextWithoutNotify(p.z.ToString("F4"));

            txt_Rot_X.SetTextWithoutNotify(r.x.ToString("F4"));
            txt_Rot_Y.SetTextWithoutNotify(r.y.ToString("F4"));
            txt_Rot_Z.SetTextWithoutNotify(r.z.ToString("F4"));

            txt_Scale_X.SetTextWithoutNotify(s.x.ToString("F4"));
            txt_Scale_Y.SetTextWithoutNotify(s.y.ToString("F4"));
            txt_Scale_Z.SetTextWithoutNotify(s.z.ToString("F4"));
        }

        private void GotValuesChanged(string arg0)
        {
            try
            {
                Vector3 p = new(
                    float.Parse(txt_Pos_X.text),
                    float.Parse(txt_Pos_Y.text),
                    float.Parse(txt_Pos_Z.text));
                Vector3 r = new(
                    float.Parse(txt_Rot_X.text),
                    float.Parse(txt_Rot_Y.text),
                    float.Parse(txt_Rot_Z.text));
                Vector3 s = new(
                    float.Parse(txt_Scale_X.text),
                    float.Parse(txt_Scale_Y.text),
                    float.Parse(txt_Scale_Z.text));

                (Woc as WOCTransform).SetState(p, r, s, G.WorldEditorData.UsingGlobal);
                PropertyPanel.CommitModification(this);
            }
            catch (FormatException) { }
        }
    }
}
