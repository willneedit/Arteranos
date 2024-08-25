/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arteranos.WorldEdit
{
    public class FirstPersonCamera : MonoBehaviour
    {
        void Update()
        {
            if(Camera.main)
            {
                Transform ct = Camera.main.transform;
                transform.SetPositionAndRotation(ct.position, ct.rotation);
            }
        }
    }
}
