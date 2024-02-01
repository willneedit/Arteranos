/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;

namespace Arteranos.Avatar
{
    public interface IAvatarBody
    {
        string GalleryModeURL { get; set; }
        bool Invisible { get; set; }
        IAvatarMeasures AvatarMeasures { get; }
        void ReloadAvatar(string url, float height, int gender);
    }
}