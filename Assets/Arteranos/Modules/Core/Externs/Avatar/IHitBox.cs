/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;

namespace Arteranos.Avatar
{
    public interface IHitBox : IMonoBehaviour
    {
        IAvatarBrain Brain { get; set; }

        bool Interactable { get; set; }

        void OnTargeted(bool inSight);
    }

}