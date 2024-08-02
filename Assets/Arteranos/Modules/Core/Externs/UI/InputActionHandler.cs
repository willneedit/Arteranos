/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Arteranos
{
    [Serializable]
    public class InputActionHandler
    {
        [NonSerialized]
        public Action<InputAction.CallbackContext> CancelCallback;

        [NonSerialized]
        public Action<InputAction.CallbackContext> PerformCallback;

        [SerializeField]
        public InputActionProperty m_ActionInput;

        public InputActionProperty ActionInput
        {
            get => m_ActionInput;
            set
            {
                if(Application.isPlaying) UnbindAction();
                m_ActionInput = value;
                if(Application.isPlaying) BindAction();
            }
        }

        private bool m_ActionBound;

        public InputActionHandler()
        {
            PerformCallback = (x) => { };
            CancelCallback = (x) => { };
        }

        public void BindAction()
        {
            if(m_ActionBound) return;

            InputAction action = m_ActionInput.action;
            if(action == null) return;

            action.performed += PerformCallback;
            action.canceled += CancelCallback;
            m_ActionBound = true;

            if(m_ActionInput.reference == null) action.Enable();
        }

        public void UnbindAction()
        {
            if(!m_ActionBound) return;

            InputAction action = m_ActionInput.action;
            if(action == null) return;

            if(m_ActionInput.reference == null) action.Disable();

            action.performed -= PerformCallback;
            action.canceled -= CancelCallback;
            m_ActionBound = false;
        }
    }
}
