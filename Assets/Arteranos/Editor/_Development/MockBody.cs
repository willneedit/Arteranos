/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

#if UNITY_EDITOR

using Arteranos.Avatar;
using Arteranos.Core;
using UnityEngine;

namespace Arteranos
{
    public class MockBody : MonoBehaviour, IAvatarBody
    {
        public bool Invisible { get; set; }

        public IAvatarMeasures AvatarMeasures => throw new System.NotImplementedException();

        public void ReloadAvatar(string url, float height)
        {
            throw new System.NotImplementedException();
        }
    }
}

#endif
