/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;

#pragma warning disable IDE1006 // Benennungsstile

namespace Arteranos.UI
{
    public interface IIPFSImage
    {
        byte[] ImageData { get; set; }
        string Path { get; set; }
        Texture texture { get; set; }
    }
}