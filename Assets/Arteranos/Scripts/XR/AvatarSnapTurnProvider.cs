/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

namespace Arteranos.XR
{
    public class AvatarSnapTurnProvider : ActionBasedSnapTurnProvider
    {
        [SerializeField]
        [Tooltip("The Input System Action that will be used to read Snap Turn data from the keyboard and mouse. Must be a Value Vector2 Control.")]
        InputActionProperty m_KeyboardMouseSnapTurnAction;
        /// <summary>
        /// The Input System Action that Unity uses to read Move data from the right hand controller. Must be a <see cref="InputActionType.Value"/> <see cref="Vector2Control"/> Control.
        /// </summary>
        public InputActionProperty KeyboardMouseSnapTurnAction
        {
            get => m_KeyboardMouseSnapTurnAction;
            set => SetInputActionProperty(ref m_KeyboardMouseSnapTurnAction, value);
        }

        [SerializeField]
        [Tooltip("Enable turning with the left controller")]
        bool m_EnableTurnLeft;
        public bool EnableTurnLeft
        {
            get => m_EnableTurnLeft; 
            set => m_EnableTurnLeft = value; 
        }

        [SerializeField]
        [Tooltip("Enable turning with the right controller")]
        bool m_EnableTurnRight;
        public bool EnableTurnRight
        {
            get => m_EnableTurnRight;
            set => m_EnableTurnRight = value; 
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

            m_KeyboardMouseSnapTurnAction.EnableDirectAction();
        }

        protected new void OnDisable()
        {
            base.OnDisable();

            m_KeyboardMouseSnapTurnAction.DisableDirectAction();
        }

        protected override Vector2 ReadInput()
        {
            Vector2 leftHandValue = leftHandSnapTurnAction.action?.ReadValue<Vector2>() ?? Vector2.zero;
            Vector2 rightHandValue = rightHandSnapTurnAction.action?.ReadValue<Vector2>() ?? Vector2.zero;
            Vector2 kmValue = m_KeyboardMouseSnapTurnAction.action?.ReadValue<Vector2>() ?? Vector2.zero;

            if (!EnableTurnLeft) leftHandValue = Vector2.zero;
            if (!EnableTurnRight) rightHandValue = Vector2.zero;

            return leftHandValue + rightHandValue + kmValue;
        }
    }
}
