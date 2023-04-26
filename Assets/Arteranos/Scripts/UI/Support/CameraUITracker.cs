/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;

public class CameraUITracker : MonoBehaviour
{
    public float m_Delay = 2.0f;
    public float m_Duration = 5.0f;
    public float m_Tolerance = 1.00f;
    public Vector3 m_offset = Vector3.forward;

    private float m_countdown;
    private GameObject m_camera;
    private float m_currentSpeed = 0.0f;
    private bool m_moving = false;

    private bool initial = true;

    // Update is called once per frame
    void Update()
    {
        m_camera = Camera.main != null ? Camera.main.gameObject : null;
        if(m_camera == null) return;

        Vector3 relOffset = m_camera.transform.rotation * m_offset;

        if(initial)
        {
            // VR guideline: approach the viewer from the front, a bit to the side.
            Vector3 iniOffset = m_camera.transform.rotation * new Vector3(5, 0, 10);
            transform.SetPositionAndRotation(m_camera.transform.position + iniOffset, m_camera.transform.rotation);
            initial = false;
            return;
        }

        float tolerance = m_moving ? 0.001f : m_Tolerance;
        float dist = Vector3.Distance(transform.position - relOffset, m_camera.transform.position);

        if(dist < tolerance)
        {
            m_countdown = m_Delay;
            m_currentSpeed = 0.0f;
            m_moving = false;
            return;
        }

        m_countdown -= Time.deltaTime;

        if(m_countdown > 0) return;

        m_moving = true;

        if(m_currentSpeed < tolerance) m_currentSpeed = tolerance;

        Vector3 target = m_camera.transform.position + relOffset;
        Vector3 destination = Vector3.Lerp(transform.position, target, -m_countdown / m_Duration);
        Quaternion destrot = Quaternion.Lerp(transform.rotation, m_camera.transform.rotation, -m_countdown / m_Duration);

        transform.SetPositionAndRotation(destination, destrot);

    }
}
