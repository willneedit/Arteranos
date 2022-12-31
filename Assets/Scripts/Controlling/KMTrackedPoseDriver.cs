using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.InputSystem;


[Serializable]
public class KMTrackedPoseDriver : MonoBehaviour
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
    public Vector3 m_EulerAngles;
    public float m_RotationSpeed;

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
    }

    void OnRotationCanceled(InputAction.CallbackContext context)
    {
        Debug.Assert(m_KMRotationBound, this);
        m_KMCurrentRotation = Vector2.zero;
    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (m_KMCurrentRotation == Vector2.zero) return;

        Vector3 x = Time.deltaTime * new Vector3(-m_KMCurrentRotation.y, m_KMCurrentRotation.x, 0) * m_RotationSpeed;
        m_EulerAngles += x;

        m_EulerAngles.x = Mathf.Clamp(m_EulerAngles.x, -80, 80);
        m_EulerAngles.y = Mathf.Clamp(m_EulerAngles.y, -80, 80);

        transform.rotation = Quaternion.Euler(m_EulerAngles);
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
