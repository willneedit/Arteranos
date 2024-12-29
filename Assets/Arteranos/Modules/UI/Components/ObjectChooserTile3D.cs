/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arteranos
{
    public class ObjectChooserTile3D : UIBehaviour
    {
        [SerializeField] private TMP_Text lbl_label;
        [SerializeField] private GameObject grp_anchor;
        [SerializeField] private Button btn_paneButton;
        [SerializeField] private Vector3 ObjectScale = Vector3.one;

        public string Label
        {
            get => lbl_label.text;
            set => lbl_label.text = value;
        }

        public GameObject Object
        {
            get => _loadedObject;
            set
            {
                Destroy( _loadedObject );
                _loadedObject = value;
                Transform t = _loadedObject.transform;

                t.SetParent(grp_anchor.transform, false);
                t.localScale = ObjectScale;
                _loadedObject.SetActive(true);
            }
        }

        public UnityAction ClickListener
        {
            set => btn_paneButton.onClick.AddListener(value);
        }

        public Vector3 Scale
        {
            get => ObjectScale;
            set => ObjectScale = value;
        }

        private GameObject _loadedObject = null;

        protected override void Awake()
        {
            base.Awake();

            // Renormalize the object's scaling
            Vector3 parentScale = grp_anchor.transform.parent.lossyScale;

            grp_anchor.transform.localScale = new(
                1 / (parentScale.x + float.Epsilon),
                1 / (parentScale.y + float.Epsilon),
                1 / (parentScale.z + float.Epsilon)
                );
        }

        protected override void OnEnable()
        {
            IEnumerator RotatePreviewCoroutine()
            {
                float angle = 0.0f;

                while (true)
                {
                    angle += Time.deltaTime * 45.0f;
                    if (angle > 360.0f) angle -= 360.0f;
                    if (_loadedObject)
                        _loadedObject.transform.localRotation = Quaternion.Euler(0.0f, angle, 0.0f);

                    yield return new WaitForEndOfFrame();
                }
            }

            base.OnEnable();

            StartCoroutine(RotatePreviewCoroutine());
        }
    }
}
