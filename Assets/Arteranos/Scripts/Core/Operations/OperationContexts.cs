/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;

using Ipfs;
using System.Collections.Generic;

namespace Arteranos.Core.Operations
{

    internal class AssetDownloaderContext : Context, IAssetDownloaderContext
    {
        public Cid Cid { get; set; } = null;
        public string TargetFile { get; set; } = null;
        public long Size { get; set; } = -1;
    }

    internal class WorldDownloaderContext : AssetDownloaderContext
    {
        public Cid WorldInfoCid = null;
        public string worldAssetBundleFile = null;
    }

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

    internal class AvatarDownloaderContext : AssetDownloaderContext, IAvatarDownloaderOptions, IAvatarMeasures, IObjectStats
    {
        public bool InstallEyeAnimation { get; set; } = false;
        public bool InstallMouthAnimation { get; set; } = false;
        public bool InstallAnimController { get; set; } = false;
        public bool InstallFootIK { get; set; } = false;
        public bool InstallHandIK { get; set; } = false;
        public bool InstallFootIKCollider { get; set; } = false;
        public bool InstallHandIKController { get; set; } = false;
        public bool ReadFootJoints { get; set; } = false;
        public bool ReadHandJoints { get; set; } = false;
        public float DesiredHeight { get; set; } = 0.0f;

        public bool? SidedCapitalized = null; // 'left' or 'Left' ?
        public int SidedPatternIndex = -1;

        public GameObject Avatar { get; set; } = null;
        public Transform LeftHand { get; set; } = null;
        public Transform RightHand { get; set; } = null;

        public Transform CenterEye { get; set; } = null;
        public Transform Head { get; set; } = null;

        public float EyeHeight { get; set; }
        public float FullHeight { get; set; }
        public float UnscaledHeight { get; set; }

        public List<MeshBlendShapeIndex> MouthOpen { get; set; }
        public List<FootIKData> Feet { get; set; }
        public List<Transform> Eyes { get; set; }
        public List<MeshBlendShapeIndex> EyeBlinkLeft { get; set; }
        public List<MeshBlendShapeIndex> EyeBlinkRight { get; set; }

        public List<string> JointNames { get; set; } // Bone names to use the network IK transmission
        public int Count { get; set; }
        public int Vertices { get; set; }
        public int Triangles { get; set; }
        public int Materials { get; set; }
        public float Rating { get; set; }
    }

    internal class ServerSearcherContext : Context
    {
        public List<ServerInfo> serverInfos = null;
        public Cid desiredWorldCid = null;
        public ServerPermissions desiredWorldPermissions = null;
        public MultiHash resultPeerID = null;
    }

    internal class AssetUploaderContext : Context
    {
        public string AssetURL = null;
        public string TempFile = null;
        public bool pin = false;
        public Cid Cid = null;
    }
}
