/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;
using System.Collections.Generic;
using GLTFast;
using Arteranos.Core;
using Arteranos.Core.Operations;

namespace Arteranos.Avatar
{
    public class AvatarDownloaderContext : AssetDownloaderContext, IAvatarDownloaderOptions, IAvatarMeasures, IObjectStats
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

        public GltfImport GltfImport { get; set; }
    }
}
