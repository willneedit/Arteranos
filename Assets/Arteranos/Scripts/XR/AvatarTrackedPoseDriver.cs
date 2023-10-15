/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;
using UnityEngine.InputSystem.XR;

namespace Arteranos.XR
{
    public class AvatarTrackedPoseDriver : TrackedPoseDriver
    {
        Vector3 OldPosition = Vector3.zero;
        Quaternion OldRotation = Quaternion.identity;

        // Skip transform update if there's no movement from the headset at all.
        protected override void SetLocalTransform(Vector3 newPosition, Quaternion newRotation)
        {
            if(newPosition == OldPosition && newRotation == OldRotation) { return; }

            base.SetLocalTransform(newPosition, newRotation);
        }
    }
}
