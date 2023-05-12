/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Arteranos.XR
{
    [Serializable]
    public class KMTrackedPoseDriver : MonoBehaviour
    {
        private bool m_KMRotationBound;

        [SerializeField]
        InputActionProperty m_KMRotationInput;
        public InputActionProperty KMRotationInput
        {
            get => m_KMRotationInput;
            set
            {
                if(Application.isPlaying) UnbindRotation();
                m_KMRotationInput = value;
                if(Application.isPlaying && isActiveAndEnabled) BindRotation();
            }
        }

        void BindRotation()
        {
            if(m_KMRotationBound) return;

            InputAction action = m_KMRotationInput.action;
            if(action == null) return;

            action.performed += OnRotationPerformed;
            action.canceled += OnRotationCanceled;
            m_KMRotationBound = true;

            if(m_KMRotationInput.reference == null)
            {
                action.Rename($"{gameObject.name} - TPD - Rotation");
                action.Enable();
            }
        }

        void UnbindRotation()
        {
            if(!m_KMRotationBound) return;

            InputAction action = m_KMRotationInput.action;
            if(action == null) return;

            if(m_KMRotationInput.reference == null) action.Disable();

            action.performed -= OnRotationPerformed;
            action.canceled -= OnRotationCanceled;
            m_KMRotationBound = false;
        }

        protected void OnEnable() => BindRotation();

        protected void OnDisable() => UnbindRotation();

        Vector2 m_KMCurrentRotation;
        public Vector3 m_EulerAngles;
        public float m_RotationSpeed;

        void OnRotationPerformed(InputAction.CallbackContext context)
        {
            Debug.Assert(m_KMRotationBound, this);
            m_KMCurrentRotation = context.ReadValue<Vector2>();
        }

        void OnRotationCanceled(InputAction.CallbackContext context)
        {
            Debug.Assert(m_KMRotationBound, this);
            m_KMCurrentRotation = Vector2.zero;
        }

        // Update is called once per frame
        void Update()
        {
            if(m_KMCurrentRotation == Vector2.zero) return;

            Vector3 x = m_RotationSpeed * Time.deltaTime * new Vector3(-m_KMCurrentRotation.y, m_KMCurrentRotation.x, 0);
            m_EulerAngles += x;

            m_EulerAngles.x = Mathf.Clamp(m_EulerAngles.x, -80, 80);
            m_EulerAngles.y = Mathf.Clamp(m_EulerAngles.y, -80, 80);

            transform.localRotation = Quaternion.Euler(m_EulerAngles);
        }

    }
}
