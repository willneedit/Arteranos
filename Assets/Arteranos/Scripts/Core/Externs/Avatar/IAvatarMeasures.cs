/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;
using System.Collections.Generic;


namespace Arteranos.Core
{
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