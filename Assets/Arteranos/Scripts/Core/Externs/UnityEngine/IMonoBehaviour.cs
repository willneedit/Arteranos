/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */


#pragma warning disable IDE1006 // Because Unity's more relaxed naming convention

namespace UnityEngine
{
    public interface IMonoBehaviour
    {
        // Behaviour
        public bool enabled { get; set; }
        public bool isActiveAndEnabled { get; }

        // Component
        public Transform transform { get; }
        public GameObject gameObject { get; }
        public string tag { get; set; }

        // Object
        public string name { get; set; }
    }
}