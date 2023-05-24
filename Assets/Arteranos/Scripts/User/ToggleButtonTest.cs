/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arteranos
{
    public class ToggleButtonTest : MonoBehaviour
    {
        private Renderer Renderer = null;
        private void Awake() => Renderer = GetComponent<Renderer>();

        public void TurnRed() => Renderer.material.color = Color.red;// renderer.material.SetColor("_Color", Color.red);

        public void TurnGreen() => Renderer.material.color = Color.green;// renderer.material.SetColor("_Color", Color.green);
    }
}
