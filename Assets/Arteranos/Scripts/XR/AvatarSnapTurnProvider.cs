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
            return base.ReadInput() +
                m_KeyboardMouseSnapTurnAction.action?.ReadValue<Vector2>() ?? Vector2.zero;
        }
    }
}
