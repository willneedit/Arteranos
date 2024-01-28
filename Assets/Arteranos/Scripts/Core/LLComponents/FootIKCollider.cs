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
using System;
using System.Collections.Generic;

namespace Arteranos.Avatar
{
    // Seriously, MUCH less boilerplate than in Ready Player Me, and much less assumptions
    // on the avatar in question...
    public class FootIKCollider : MonoBehaviour
    {
        public float Elevation = 0;
        public Transform rootTransform = null;

        public void LateUpdate() 
            => AdjustFootIK(transform);

        private void AdjustFootIK(Transform foot)
        {
            // Everything except Layers 17 and 18 (BubbleFriend and BubbleStranger)
            int layerMask = ~((1 << 17) | (1 << 18));

            Vector3 upVector = rootTransform.rotation * Vector3.up;
            float scale = rootTransform.localScale.y;

            // FIXME Maybe with a 'falling' animation would the IK switched off.

            // Larger avatars can lift their knees higher than smaller avatars
            float maxLiftKnees = 0.50f * scale;

            Ray ray = new(foot.position + upVector * maxLiftKnees, -upVector);

            if (Physics.SphereCast(ray, Elevation, out RaycastHit hitInfo, 0.50f, layerMask))
            {
                foot.SetPositionAndRotation(hitInfo.point + upVector * Elevation,
                    Quaternion.FromToRotation(upVector, hitInfo.normal) * foot.rotation);
            }
        }
    }
}
