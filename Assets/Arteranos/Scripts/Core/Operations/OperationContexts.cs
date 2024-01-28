/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using UnityEngine;

using Arteranos.Core;
using System.Threading;
using Utils = Arteranos.Core.Utils;
using Ipfs;
using Arteranos.Services;
using System.Collections.Generic;

namespace Arteranos.Core.Operations
{
    public interface IAssetDownloaderContext
    {
        Cid Cid { get; set; }
        long Size { get; set; }
        string TargetFile { get; set; }
    }

    public struct MeshBlendShapeIndex
    {
        public SkinnedMeshRenderer Renderer;
        public int Index;
    }

    public struct FootIKData
    {
        public Transform FootTransform;
        public float Elevation;
    }

    public interface IObjectStats
    {
        int Count { get; set; }
        int Vertices { get; set; }
        int Triangles { get; set; }
        int Materials { get; set; }
        float Rating { get; set; } // Normalized, more is better
    }
    public interface IAvatarMeasures
    {
        Transform CenterEye { get; } // The position, as close as much to match the headset
        float EyeHeight { get; } // Distance of the eyes to the floor
        float FullHeight { get; }
        float UnscaledHeight { get; } // = FullHeight only with scale = 1
        Transform Head { get; }
        Transform LeftHand { get; }
        Quaternion LhrOffset { get; }
        Quaternion RhrOffset { get; }
        Transform RightHand { get; }
        List<string> JointNames { get; set; }
        List<FootIKData> Feet { get; set; } // The feet to handle with IK
        List<Transform> Eyes { get; } // The eyes to roll/move
        List<MeshBlendShapeIndex> MouthOpen { get; } // Blend shape(s) to make the mouth opeen
        List<MeshBlendShapeIndex> EyeBlinkLeft { get; } // Blend shape(s) to make the eye(s) closed
        List<MeshBlendShapeIndex> EyeBlinkRight { get; } // Blend shape(s) to make the eye(s) closed
    }

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
        bool InstallAvatarController { get; set; }
        bool InstallFootIK { get; set; }
        bool InstallHandIK { get; set; }
        float DesiredHeight { get; set; }
    }

    public class AvatarDownloaderOptions : IAvatarDownloaderOptions
    {
        public bool InstallEyeAnimation { get; set; }
        public bool InstallAvatarController { get; set; }
        public bool InstallFootIK { get; set; }
        public bool InstallHandIK { get; set; }
        public float DesiredHeight { get; set; }
    }

    internal class AvatarDownloaderContext : AssetDownloaderContext, IAvatarDownloaderOptions, IAvatarMeasures, IObjectStats
    {
        public bool InstallEyeAnimation { get; set; } = false;
        public bool InstallAvatarController { get; set; } = false;
        public bool InstallFootIK { get; set; } = false;
        public bool InstallHandIK { get; set; } = false;
        public float DesiredHeight { get; set; } = 0.0f;

        public GameObject Avatar = null;
        public bool? SidedCapitalized = null; // 'left' or 'Left' ?
        public int SidedPatternIndex = -1;

        public Transform LeftHand { get; set; } = null;
        public Transform RightHand { get; set; } = null;
        public Quaternion LhrOffset { get; set; } = Quaternion.identity;
        public Quaternion RhrOffset { get; set; } = Quaternion.identity;

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
