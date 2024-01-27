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
        public IAvatarMeasures AvatarMeasures = null;

        public void LateUpdate() 
            => AdjustFootIK(transform);

        private void AdjustFootIK(Transform foot)
        {
            // Everything except Layers 17 and 18 (BubbleFriend and BubbleStranger)
            int layerMask = ~((1 << 17) | (1 << 18));

            // FIXME Unscaled avatar size in relation
            // FIXME Custom up vector
            // FIXME Maybe with a 'falling' animation would the IK switched off.
            // If the avatar is a midget, he cannot lift his feet half a meter up, so sacle down accordingly.
            float maxLiftKnees = 0.50f; // * (AvatarMeasures.OriginalFullHeight / AvatarMeasures.FullHeight);

            Ray ray = new(foot.position + Vector3.up * maxLiftKnees, Vector3.down);

            if (Physics.SphereCast(ray, AvatarMeasures.FootElevation, out RaycastHit hitInfo, 0.50f, layerMask))
            {
                foot.SetPositionAndRotation(hitInfo.point + Vector3.up * AvatarMeasures.FootElevation,
                    Quaternion.FromToRotation(Vector3.up, hitInfo.normal) * foot.rotation);
            }
        }
    }
}
