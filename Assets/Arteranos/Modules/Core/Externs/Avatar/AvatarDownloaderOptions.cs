/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

namespace Arteranos.Avatar
{
    public class AvatarDownloaderOptions : IAvatarDownloaderOptions
    {
        public bool InstallEyeAnimation { get; set; }
        public bool InstallMouthAnimation { get; set; }
        public bool InstallAnimController { get; set; }
        public bool InstallFootIK { get; set; }
        public bool InstallHandIK { get; set; }
        public bool InstallFootIKCollider { get; set; }
        public bool InstallHandIKController { get; set; }
        public bool ReadFootJoints { get; set; }
        public bool ReadHandJoints { get; set; }
        public float DesiredHeight { get; set; }
    }
}
