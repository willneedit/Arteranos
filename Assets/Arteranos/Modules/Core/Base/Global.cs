/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Avatar;
using Arteranos.Core;
using Arteranos.Services;
using Arteranos.UI;
using Arteranos.WorldEdit;
using Arteranos.XR;
using Ipfs;

namespace Arteranos
{
    public static class G
    {
        public class World_
        {
            public Cid Cid { get; set; }
            public string Name { get; set; }
        }

        public class DefaultAvatar_
        {
            public Cid Male { get; set; } = null;
            public Cid Female { get; set; } = null;

        }

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
        public static ITransitionProgress TransitionProgress { get; set; }
        public static IAvatarDownloader AvatarDownloader { get; set; }
        // ---------------------------------------------------------------
        public static World_ World { get; } = new();
        // ---------------------------------------------------------------
        public static Client Client { get; set; }
        public static Server Server { get; set; }
        public static ServerUserBase ServerUsers { get; set; }
        // ---------------------------------------------------------------
        public static DefaultAvatar_ DefaultAvatar { get; } = new();
    }
}