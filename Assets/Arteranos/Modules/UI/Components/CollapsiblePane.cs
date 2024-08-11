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
    public class CollapsiblePane : UIBehaviour
    {
        public Button Btn_expand;
        public TextMeshProUGUI Lbl_title;

        public string Title { get => Lbl_title.text; set => Lbl_title.text = value; }
        public bool IsOpen
        {
            get => isOpen;
            set
            {
                bool old = isOpen;
                isOpen = value;
                if (old != isOpen) SetOpenState(true);
            }
        }

        [SerializeField] private bool isOpen;

        private Canvas Canvas;
        private RectTransform CollapseTransform;

        protected override void Awake()
        {
            base.Awake();

            Canvas = GetComponentInParent<Canvas>();
            Debug.Assert(Canvas);

            CollapseTransform = transform.GetChild(1) as RectTransform;
            Debug.Assert(CollapseTransform);

            Btn_expand.onClick.AddListener(() =>
            {
                isOpen = !isOpen;
                SetOpenState(false);
            });
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            SetOpenState(true);
        }

        private void SetOpenState(bool instantly)
        {
            IEnumerator AnimateOpen(float duration)
            {
                Quaternion btnrotation = Btn_expand.transform.localRotation;
                Vector3 colScale = CollapseTransform.localScale;

                Quaternion tgtLocalRotation = isOpen
                    ? Quaternion.Euler(0f, 0f, -90.0f)
                    : Quaternion.identity;

                Vector3 tgtLocalScale = isOpen
                    ? Vector3.one
                    : new(1.0f, 0.0f, 1.0f);

                float elapsed = 0f;

                while(elapsed < duration)
                {
                    elapsed += Time.deltaTime;

                    elapsed = Mathf.Clamp(elapsed, 0f, duration);

                    Btn_expand.transform.localRotation = Quaternion.Lerp(btnrotation, tgtLocalRotation, elapsed / duration);
                    CollapseTransform.localScale = Vector3.Lerp(colScale, tgtLocalScale, elapsed / duration);

                    LayoutRebuilder.MarkLayoutForRebuild(Canvas.transform as RectTransform);
                    yield return new WaitForEndOfFrame();
                }
            }

            StopAllCoroutines();

            StartCoroutine(AnimateOpen(instantly ? 0.0001f : 0.5f));
        }
    }
}
