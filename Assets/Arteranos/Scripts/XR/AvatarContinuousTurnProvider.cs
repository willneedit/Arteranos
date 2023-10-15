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
    public class AvatarContinuousTurnProvider : ActionBasedContinuousTurnProvider
    {
        [SerializeField]
        [Tooltip("The Input System Action that will be used to read Turn data from the keyboard and mouse. Must be a Value Vector2 Control.")]
        InputActionProperty m_KeyboardMouseTurnAction;
        /// <summary>
        /// The Input System Action that Unity uses to read Move data from the right hand controller. Must be a <see cref="InputActionType.Value"/> <see cref="Vector2Control"/> Control.
        /// </summary>
        public InputActionProperty KeyboardMouseTurnAction
        {
            get => m_KeyboardMouseTurnAction;
            set => SetInputActionProperty(ref m_KeyboardMouseTurnAction, value);
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

            m_KeyboardMouseTurnAction.EnableDirectAction();
        }

        protected new void OnDisable()
        {
            base.OnDisable();

            m_KeyboardMouseTurnAction.DisableDirectAction();
        }

        protected override Vector2 ReadInput()
        {
            return base.ReadInput() +
                m_KeyboardMouseTurnAction.action?.ReadValue<Vector2>() ?? Vector2.zero;
        }
    }
}
