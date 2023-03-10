/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public class MouseController : MonoBehaviour
{

    [SerializeField]
    InputActionProperty m_KMRotationInput;
   public InputActionProperty KMRotationInput
    {
        get => m_KMRotationInput;
        set 
        {
            if (Application.isPlaying)
                UnbindRotation();

            m_KMRotationInput = value;

            if (Application.isPlaying && isActiveAndEnabled)
                BindRotation();

        }
    }

    bool m_KMRotationBound;
    Vector2 m_KMCurrentRotation;

    void BindRotation()
    {
        if (m_KMRotationBound)
            return;

        var action = m_KMRotationInput.action;
        if (action == null)
            return;

        action.performed += OnRotationPerformed;
        action.canceled += OnRotationCanceled;
        m_KMRotationBound = true;

        if (m_KMRotationInput.reference == null)
        {
            action.Rename($"{gameObject.name} - TPD - Rotation");
            action.Enable();
        }
    }

    void UnbindRotation()
    {
        if (!m_KMRotationBound)
            return;

        var action = m_KMRotationInput.action;
        if (action == null)
            return;

        if (m_KMRotationInput.reference == null)
            action.Disable();

        action.performed -= OnRotationPerformed;
        action.canceled -= OnRotationCanceled;
        m_KMRotationBound = false;
    }

    void OnRotationPerformed(InputAction.CallbackContext context)
    {
        Debug.Assert(m_KMRotationBound, this);
        m_KMCurrentRotation = context.ReadValue<Vector2>();
        Ray castPoint = Camera.main.ScreenPointToRay(m_KMCurrentRotation);

        Quaternion q = Quaternion.LookRotation(castPoint.direction, Vector3.up);
        transform.rotation = q;

    }

    void OnRotationCanceled(InputAction.CallbackContext context)
    {
        Debug.Assert(m_KMRotationBound, this);
        m_KMCurrentRotation = Vector2.zero;
    }

    /// <summary>
    /// This function is called when the object becomes enabled and active.
    /// </summary>
    protected void OnEnable()
    {
        BindRotation();
    }

    /// <summary>
    /// This function is called when the object becomes disabled or inactive.
    /// </summary>
    protected void OnDisable()
    {
        UnbindRotation();
    }


}
