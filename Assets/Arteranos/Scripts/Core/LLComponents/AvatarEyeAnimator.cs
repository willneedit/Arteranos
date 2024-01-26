/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;

using Arteranos.Core.Operations;
using Random = UnityEngine.Random;
using System.Collections;

namespace Arteranos.Avatar
{
    // Seriously, MUCH less boilerplate than in Ready Player Me, and much less assumptions
    // on the avatar in question...
    public class AvatarEyeAnimator : MonoBehaviour
    {
        private const int VERTICAL_MARGIN = 15;
        private const int HORIZONTAL_MARGIN = 5;

        public float blinkDurationLow = 0.1f;
        public float blinkDurationHigh = 0.15f;

        public float blinkIntervalLow = 4.0f;
        public float blinkIntervalHigh = 6.0f;

        public IAvatarMeasures AvatarMeasures = null;

        private void Start()
        {
            StartCoroutine(AnimateEyes());
        }

        private IEnumerator AnimateEyes()
        {
            while(true)
            {
                yield return new WaitForSeconds(Random.Range(blinkIntervalLow, blinkIntervalHigh));

                SetEyesClosedState(1.0f);
                RotateEyes();

                yield return new WaitForSeconds(Random.Range(blinkDurationLow, blinkDurationHigh));

                SetEyesClosedState(0.0f);
            }
        }

        private void SetEyesClosedState(float state)
        {
            foreach(MeshBlendShapeIndex mbsi in AvatarMeasures.EyeBlinkLeft)
                mbsi.Renderer.SetBlendShapeWeight(mbsi.Index, state);

            foreach (MeshBlendShapeIndex mbsi in AvatarMeasures.EyeBlinkRight)
                mbsi.Renderer.SetBlendShapeWeight(mbsi.Index, state);
        }

        // Rotate all of the eyes
        private void RotateEyes()
        {
            Quaternion rot = GetRandomLookRotation();
            foreach (Transform eye in AvatarMeasures.Eyes)
                eye.localRotation = rot;
        }

        private Quaternion GetRandomLookRotation() 
            => Quaternion.Euler(
                Random.Range(-HORIZONTAL_MARGIN, HORIZONTAL_MARGIN), 
                Random.Range(-VERTICAL_MARGIN, VERTICAL_MARGIN), 
                0);

    }
}