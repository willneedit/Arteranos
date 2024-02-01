/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

#if UNITY_EDITOR

using Arteranos.Avatar;
using Arteranos.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Arteranos
{
    public class MockBody : MonoBehaviour, IAvatarBody
    {
        public string GalleryModeURL { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public bool Invisible { get; set; }

        public IAvatarMeasures AvatarMeasures => throw new System.NotImplementedException();

        public void ReloadAvatar(string url, float height, int gender)
        {
            throw new System.NotImplementedException();
        }
    }
}

#endif
