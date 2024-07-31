/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Ipfs;
using UnityEngine;


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


}