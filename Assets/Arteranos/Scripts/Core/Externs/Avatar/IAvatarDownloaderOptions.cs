/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

namespace Arteranos.Avatar
{
    public interface IAvatarDownloaderOptions
    {
        bool InstallEyeAnimation { get; set; }
        bool InstallMouthAnimation { get; set; }
        bool InstallAnimController { get; set; }
        bool InstallFootIK { get; set; }
        bool InstallHandIK { get; set; }
        bool InstallFootIKCollider { get; set; }
        bool InstallHandIKController { get; set; }
        bool ReadFootJoints { get; set; }
        bool ReadHandJoints { get; set; }
        float DesiredHeight { get; set; }
    }
}
