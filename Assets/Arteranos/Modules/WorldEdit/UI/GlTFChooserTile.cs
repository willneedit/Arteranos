/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Arteranos.Core.Managed;
using System.Threading;

namespace Arteranos.WorldEdit
{
    public class GlTFChooserTile : UIBehaviour
    {
        public string GLTFObjectPath { get; set; } = null;
        public GameObject LoadedObject { get; private set; } = null;

        public Button btn_PaneButton;
        public GameObject grp_ObjectAnchor;

        protected override void Start()
        {
            IEnumerator Cor()
            {
                using CancellationTokenSource cts = new(10000);
                IPFSGLTFObject obj = new(GLTFObjectPath, cts.Token);
                obj.InitActive = false;

                yield return obj.GameObject.WaitFor();
                LoadedObject = obj.GameObject;
                Bounds? b = obj.Bounds;

                // Debug.Log($"Bounds: Center={b.Value.center}, Extent={b.Value.extents}, Max={b.Value.max}");

                // Center is locked to the preview anchors, now only extents matter.
                float largestAxis = b.Value.extents.x;
                if (b.Value.extents.y > largestAxis) largestAxis = b.Value.extents.y;
                if (b.Value.extents.z > largestAxis) largestAxis = b.Value.extents.z;

                Transform t = LoadedObject.transform;
                grp_ObjectAnchor.transform.localScale *= 0.20f / (largestAxis + float.Epsilon);
                t.SetParent(grp_ObjectAnchor.transform, false);

                t.localPosition = -b.Value.center;

                LoadedObject.SetActive(true);
            }

            base.Start();

            StartCoroutine(Cor());
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
                    if (LoadedObject)
                        LoadedObject.transform.localRotation = Quaternion.Euler(0.0f, angle, 0.0f);

                    yield return new WaitForEndOfFrame();
                }
            }

            base.OnEnable();

            StartCoroutine(RotatePreviewCoroutine());
        }
    }
}
