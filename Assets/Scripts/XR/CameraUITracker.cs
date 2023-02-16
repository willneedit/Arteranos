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

    private Vector3 m_offset;
    private float m_countdown;
    private GameObject m_camera;
    private float m_currentSpeed = 0.0f;
    private bool m_moving = false;

    // Start is called before the first frame update
    void Start()
    {
        m_camera = GameObject.Find("_AvatarView");

        if(m_camera == null)
            m_camera = Camera.main.gameObject;

        m_offset = transform.position - m_camera.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 relOffset = m_camera.transform.rotation * m_offset;

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

        transform.position = destination;
        transform.rotation = destrot;

    }
}
