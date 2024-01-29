/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;

namespace Arteranos.Avatar
{
    public interface IAvatarLoader : IAvatarMeasures
    {
        string GalleryModeURL { get; set; }
        bool Invisible { get; set; }
        float OriginalFullHeight { get; }

        void RequestAvatarHeightChange(float targetHeight);
        void RequestAvatarURLChange(string current);

        /// <summary>
        /// Reset the the avatar to an 'Attention' pose
        /// </summary>
        public void ResetPose(bool leftHand, bool rightHand);

        /// <summary>
        /// Update the mouth open/closed state
        /// </summary>
        /// <param name="amount">Normalized state, with 1 being fully open</param>
        public void UpdateOpenMouth(float amount);
    }
}