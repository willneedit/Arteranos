/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;

#pragma warning disable IDE1006 // Because Unity's more relaxed naming convention

namespace Arteranos.Avatar
{
    public interface IHitBox
    {
        GameObject gameObject { get; }
        IAvatarBrain Brain { get; set; }

        bool interactable { get; set; }

        void OnTargeted(bool inSight);
    }

}