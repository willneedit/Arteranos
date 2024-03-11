/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;

using Ipfs;
using System.Collections.Generic;
using System;

namespace Arteranos.Core.Operations
{

    internal class AssetDownloaderContext : Context, IAssetDownloaderContext
    {
        public string path { get; set; } = null;
        public bool isTarred { get; set; } = false;
        public string TargetFile { get; set; } = null;
        public long Size { get; set; } = -1;
    }

    [Obsolete("Transition to WorldInfoContext and WorldAssetContext")]
    internal class WorldDownloaderContext : AssetDownloaderContext
    {
        public string WorldInfoCid = null;
        public string WorldAssetBundleFile = null;
    }

    internal class WorldDownloadContext : Context
    {
        public Cid WorldCid;
        public WorldInfo WorldInfo;
        public string WorldAssetBundlePath;
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
        public bool asTarred = false;           // Asset (ZIP) archive will be unpacked and published as a directory
        public string AssetURL = null;          // Local file, file: URL, http(s): URL, resource: URL
        public string TempFile = null;
        public bool pin = false;
        public Cid Cid = null;                  // Contents as the file as-is, or the directory repacked as tar, or the root for <cid>/path/to/file.ext
    }
}
