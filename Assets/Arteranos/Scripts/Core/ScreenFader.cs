/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Arteranos
{
    public class ScreenFader : MonoBehaviour
    {

        [SerializeField] private Image faderImage = null;

        private float elapsed = 0.0f;
        private float currentOpacity = 0.0f;
        private float targetOpacity = 0.0f;
        private float duration = 0.0f;

        void Update()
        {
            if(elapsed >= duration) return;

            elapsed += Time.deltaTime;

            float normalizedProgress = elapsed / duration;

            float alpha = Mathf.Lerp(currentOpacity, targetOpacity, normalizedProgress);
            alpha = Mathf.Clamp01(alpha);

            faderImage.color = new Color(faderImage.color.r, faderImage.color.g, faderImage.color.b, alpha);

            // Completely switch off the image to save an interference of the transparency
            GetComponent<Canvas>().enabled = alpha > 0.0f;
        }

        public static void StartFading(float opacity, float duration = 0.5f)
        {
            ScreenFader sf = FindObjectOfType<ScreenFader>();

            sf.elapsed = 0.0f;
            sf.duration= duration;
            sf.targetOpacity= opacity;

            sf.currentOpacity = sf.faderImage.color.a;
        }
    }
}
