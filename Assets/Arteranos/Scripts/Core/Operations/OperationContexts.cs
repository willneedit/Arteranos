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

    public class MeshBlendShapeIndex
    {
        public SkinnedMeshRenderer Renderer { get; set; } = null;
        public int Index { get; set; } = -1;
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
        float FootElevation { get; } // Close to zero, but with plateau shoes or hooves...
        float FullHeight { get; }
        Transform Head { get; }
        Transform LeftFoot { get; }
        Transform LeftHand { get; }
        Quaternion LhrOffset { get; }
        Quaternion RhrOffset { get; }
        Transform RightFoot { get; }
        Transform RightHand { get; }
        List<MeshBlendShapeIndex> MouthOpen { get; } // Blend shape(s) to make the mouth opeen
        List<Transform> Eyes { get; } // The eyes to roll/move
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

    internal class AvatarDownloaderContext : AssetDownloaderContext, IAvatarMeasures, IObjectStats
    {
        public GameObject Avatar { get; set; } = null;
        public bool? SidedCapitalized { get; set; } = null; // 'left' or 'Left' ?
        public int SidedPatternIndex { get; set; } = -1;
        public Transform LeftHand { get; set; } = null;
        public Transform RightHand { get; set; } = null;
        public Transform LeftFoot { get; set; } = null;
        public Transform RightFoot { get; set; } = null;

        public Quaternion LhrOffset { get; set; } = Quaternion.identity;
        public Quaternion RhrOffset { get; set; } = Quaternion.identity;

        public Transform CenterEye { get; set; } = null;
        public Transform Head { get; set; } = null;

        public float FootElevation { get; set; }
        public float EyeHeight { get; set; }
        public float FullHeight { get; set; }

        public List<MeshBlendShapeIndex> MouthOpen { get; set; }
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
