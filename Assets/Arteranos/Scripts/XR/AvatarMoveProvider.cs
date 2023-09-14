/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace Arteranos.XR
{
    public class AvatarMoveProvider : ActionBasedContinuousMoveProvider
    {
        protected override Vector3 ComputeDesiredMove(Vector2 input)
        {
            if (input == Vector2.zero)
                return Vector3.zero;

            var xrOrigin = system.xrOrigin;
            if (xrOrigin == null)
                return Vector3.zero;

            // Assumes that the input axes are in the range [-1, 1].
            // Clamps the magnitude of the input direction to prevent faster speed when moving diagonally,
            // while still allowing for analog input to move slower (which would be lost if simply normalizing).
            var inputMove = Vector3.ClampMagnitude(new Vector3(0f, 0f, input.y), 1f);

            var originTransform = xrOrigin.Origin.transform;
            var originUp = originTransform.up;

            // Determine frame of reference for what the input direction is relative to
            var forwardSourceTransform = forwardSource == null ? xrOrigin.Camera.transform : forwardSource;
            var inputForwardInWorldSpace = forwardSourceTransform.forward;
            if (Mathf.Approximately(Mathf.Abs(Vector3.Dot(inputForwardInWorldSpace, originUp)), 1f))
            {
                // When the input forward direction is parallel with the rig normal,
                // it will probably feel better for the player to move along the same direction
                // as if they tilted forward or up some rather than moving in the rig forward direction.
                // It also will probably be a better experience to at least move in a direction
                // rather than stopping if the head/controller is oriented such that it is perpendicular with the rig.
                inputForwardInWorldSpace = -forwardSourceTransform.up;
            }

            Vector3 inputForwardProjectedInWorldSpace = Vector3.ProjectOnPlane(inputForwardInWorldSpace, originUp);

            // As long you're obeying gravity, you're stuck to the ground plane.

            Quaternion forwardRotation = useGravity
                ? Quaternion.FromToRotation(originTransform.forward, inputForwardProjectedInWorldSpace)
                : forwardSourceTransform.localRotation;
            Vector3 translationInRigSpace = forwardRotation * inputMove * (moveSpeed * Time.deltaTime);
            Vector3 translationInWorldSpace = originTransform.TransformDirection(translationInRigSpace);

            return translationInWorldSpace;
        }

        protected override void MoveRig(Vector3 translationInWorldSpace)
        {
            base.MoveRig(translationInWorldSpace);
        }
    }
}
