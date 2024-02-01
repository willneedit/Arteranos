/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

#if UNITY_EDITOR

using Arteranos.Avatar;
using Arteranos.Core;
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

        public float OriginalFullHeight => throw new System.NotImplementedException();

        public List<MeshBlendShapeIndex> MouthOpen => throw new System.NotImplementedException();

        public List<Transform> Eyes => throw new System.NotImplementedException();

        public List<MeshBlendShapeIndex> EyeBlinkLeft => throw new System.NotImplementedException();

        public List<MeshBlendShapeIndex> EyeBlinkRight => throw new System.NotImplementedException();

        public List<string> JointNames { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public List<FootIKData> Feet { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public float UnscaledHeight => throw new System.NotImplementedException();

        public void ReloadAvatar(string url, float height, int gender)
        {
            throw new System.NotImplementedException();
        }

        public void RequestAvatarHeightChange(float targetHeight)
        {
            throw new System.NotImplementedException();
        }

        public void RequestAvatarURLChange(string current)
        {
            throw new System.NotImplementedException();
        }

        public void ResetPose(bool leftHand, bool rightHand) => throw new System.NotImplementedException();
        public void UpdateOpenMouth(float amount) => throw new System.NotImplementedException();
    }
}

#endif
