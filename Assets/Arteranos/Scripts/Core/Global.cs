/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Avatar;
using Arteranos.Services;
using Arteranos.UI;
using Arteranos.Web;
using Arteranos.WorldEdit;
using Arteranos.XR;

namespace Arteranos
{
    public static class G
    {
        public static IWorldEditorData WorldEditorData { get; set; }

        public static ISceneLoader SceneLoader { get; set; }

        public static IXRControl XRControl { get; set; }

        public static IXRVisualConfigurator XRVisualConfigurator { get; set; }

        public static IAvatarBrain Me {  get; set; }

        public static INetworkStatus NetworkStatus { get; set; }

        public static IAudioManager AudioManager { get; set; }

        public static IConnectionManager ConnectionManager { get; set; }

        public static ISysMenu SysMenu { get; set; }

        public static IIPFSService IPFSService { get; set; }
    }
}