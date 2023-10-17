/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

#if UNITY_EDITOR

using Arteranos.Avatar;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arteranos
{
    public class MockBody : MonoBehaviour, IAvatarLoader
    {
        public Transform LeftHand => throw new System.NotImplementedException();

        public Transform RightHand => throw new System.NotImplementedException();

        public Transform LeftFoot => throw new System.NotImplementedException();

        public Transform RightFoot => throw new System.NotImplementedException();

        public Quaternion LhrOffset => throw new System.NotImplementedException();

        public Quaternion RhrOffset => throw new System.NotImplementedException();

        public Transform CenterEye => throw new System.NotImplementedException();

        public Transform Head => throw new System.NotImplementedException();

        public float FootElevation => throw new System.NotImplementedException();

        public string GalleryModeURL { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public float EyeHeight => 1.85f;

        public float FullHeight => 1.75f;

        public bool Invisible { get; set; }

        public void ResetPose(bool leftHand, bool rightHand) => throw new System.NotImplementedException();
        public void UpdateOpenMouth(float amount) => throw new System.NotImplementedException();
    }
}

#endif
