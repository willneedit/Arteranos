/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arteranos
{
    public class LifetimeTestComponent : MonoBehaviour
    {
        public bool CalledAwake { get; private set; } = false;
        public bool CalledStart { get; private set; } = false;
        public bool CalledOnEnable { get; private set; } = false;
        public bool CalledOnDisable { get; private set; } = false;
        public bool CallecdOnDestroy { get; private set; } = false;

        private void Awake()
        {
            CalledAwake = true;
        }

        void Start()
        {
            CalledStart = true;
        }


        private void OnEnable()
        {
            CalledOnEnable = true;
        }

        private void OnDisable()
        {
            CalledOnDisable = true;
        }
    }
}
