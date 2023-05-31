/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using UnityEngine;

namespace Arteranos.Avatar
{
    public interface IAvatarLoader
    {
        Transform LeftHand { get; }
        Transform RightHand { get; }
        Transform LeftFoot { get; }
        Transform RightFoot { get; }

        Quaternion LhrOffset { get; }
        Quaternion RhrOffset { get; }


        Transform CenterEye { get; }
        Transform Head { get; }

        float FootElevation { get; }
        string GalleryModeURL { get; set; }
        float EyeHeight { get; }
        float FullHeight { get; }
        bool invisible { get; set; }

        /// <summary>
        /// Reset the the avatar to an 'Attention' pose
        /// </summary>
        public void ResetPose();

        /// <summary>
        /// Update the mouth open/closed state
        /// </summary>
        /// <param name="amount">Normalized state, with 1 being fully open</param>
        public void UpdateOpenMouth(float amount);
    }
}