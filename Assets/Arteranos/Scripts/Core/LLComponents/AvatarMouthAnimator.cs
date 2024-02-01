/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;

using Random = UnityEngine.Random;
using System.Collections;
using Arteranos.Core;

namespace Arteranos.Avatar
{
    public class AvatarMouthAnimator : MonoBehaviour
    {
        private const int AMPLITUDE_MULTIPLIER = 50;

        public IAvatarMeasures AvatarMeasures = null;

        public float MouthOpen { get; set; } = 0;

        private void Update()
        {
            foreach(MeshBlendShapeIndex mouthOpen in AvatarMeasures.MouthOpen)
            {
                float value = Mathf.Clamp01(MouthOpen * AMPLITUDE_MULTIPLIER);
                mouthOpen.Renderer.SetBlendShapeWeight(mouthOpen.Index, value);
            }
        }
    }
}