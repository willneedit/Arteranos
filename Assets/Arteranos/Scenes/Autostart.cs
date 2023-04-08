/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Autostart : MonoBehaviour
{
    public void Awake()
    {
        Debug.Log("I'm here!");
    }

    public void Update()
    {
        transform.Rotate(0, Time.deltaTime * 10, 0);
    }
}
