﻿/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using UnityEngine;

namespace Arteranos.NetworkIO
{
    public interface IAvatarLoader
    {
        public Transform LeftHand { get; }
        public Transform RightHand { get; }
        public Transform LeftFoot { get; }
        public Transform RightFoot { get; }

        public Quaternion LhrOffset { get; }
        public Quaternion RhrOffset { get; }


        public Transform CenterEye { get; }
        public Transform Head { get; }

        public float FootElevation { get; }

        public abstract void ResetPose();
    }
}