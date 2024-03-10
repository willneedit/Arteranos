/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Ipfs;
using UnityEngine;
using System.Collections.Generic;


namespace Arteranos.Core
{
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

    public interface IAssetDownloaderContext
    {
        string path { get; set; }
        bool isTarred { get; set; }
        long Size { get; set; }
        string TargetFile { get; set; }
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
        GameObject Avatar { get; }
        Transform CenterEye { get; } // The position, as close as much to match the headset
        float EyeHeight { get; } // Distance of the eyes to the floor
        float FullHeight { get; }
        float UnscaledHeight { get; } // = FullHeight only with scale = 1
        Transform Head { get; }
        Transform LeftHand { get; }
        Transform RightHand { get; }
        List<string> JointNames { get; }
        List<FootIKData> Feet { get; } // The feet to handle with IK
        List<Transform> Eyes { get; } // The eyes to roll/move
        List<MeshBlendShapeIndex> MouthOpen { get; } // Blend shape(s) to make the mouth opeen
        List<MeshBlendShapeIndex> EyeBlinkLeft { get; } // Blend shape(s) to make the eye(s) closed
        List<MeshBlendShapeIndex> EyeBlinkRight { get; } // Blend shape(s) to make the eye(s) closed
    }


}