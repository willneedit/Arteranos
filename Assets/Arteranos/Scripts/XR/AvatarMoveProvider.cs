/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

namespace Arteranos.XR
{
    public class AvatarMoveProvider : ActionBasedContinuousMoveProvider
    {
        [SerializeField]
        [Tooltip("The Input System Action that will be used to read Move data from the keyboard and mouse. Must be a Value Vector2 Control.")]
        InputActionProperty m_KeyboardMouseMoveAction;
        /// <summary>
        /// The Input System Action that Unity uses to read Move data from the right hand controller. Must be a <see cref="InputActionType.Value"/> <see cref="Vector2Control"/> Control.
        /// </summary>
        public InputActionProperty KeyboardMouseMoveAction
        {
            get => m_KeyboardMouseMoveAction;
            set => SetInputActionProperty(ref m_KeyboardMouseMoveAction, value);
        }

        void SetInputActionProperty(ref InputActionProperty property, InputActionProperty value)
        {
            if (Application.isPlaying)
                property.DisableDirectAction();

            property = value;

            if (Application.isPlaying && isActiveAndEnabled)
                property.EnableDirectAction();
        }

        protected new void OnEnable()
        {
            base.OnEnable();

            m_KeyboardMouseMoveAction.EnableDirectAction();
        }

        protected new void OnDisable()
        {
            base.OnDisable();

            m_KeyboardMouseMoveAction.DisableDirectAction();
        }

        protected override Vector2 ReadInput()
        {
            return base.ReadInput() +
                m_KeyboardMouseMoveAction.action?.ReadValue<Vector2>() ?? Vector2.zero;
        }

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

        private Vector3? m_LastGrounded = null;
        private CharacterController m_cc = null;
        private bool saved = false;

        protected override void MoveRig(Vector3 translationInWorldSpace)
        {
            XROrigin xrOrigin = system.xrOrigin;
            if (xrOrigin == null)
            {
                base.MoveRig(translationInWorldSpace);
                return;
            }

            if(m_cc == null)
                m_cc = xrOrigin.Origin.GetComponent<CharacterController>();

            Transform originTransform = xrOrigin.Origin.transform;

            if (m_LastGrounded == null) 
                m_LastGrounded = originTransform.position;


            // Update if the avatar is safe: grounded or floating -- not falling.
            if (m_cc.isGrounded || !useGravity)
            {
                m_LastGrounded = originTransform.position;
                saved = false;
            }

            // Too far below of the last safe position
            if (originTransform.position.y - m_LastGrounded.Value.y < -100.0f)
            {
                // Looks like a spawn point in the void if the avatar never touched
                // ground since the time he's been dropped off of it.
                // World creator's concern, then.
                if(saved) useGravity = false;

                m_LastGrounded = originTransform.position;
                // Hard move, we have to bypass the locomotion system to prevent the
                // ongoing move process cluttering the system.
                //
                // Just like with games when the character is stuck, unable to move while
                // being in the eternal falling animation.
                XRControl.Instance.MoveRig();
                saved = true;
            }

            base.MoveRig(translationInWorldSpace);
        }
    }
}
