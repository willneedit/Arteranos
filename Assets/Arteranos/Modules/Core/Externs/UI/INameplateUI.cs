/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Avatar;
using UnityEngine;

#pragma warning disable IDE1006 // Because Unity's more relaxed naming convention

namespace Arteranos.UI
{
    public interface INameplateUI
    {
        IAvatarBrain Bearer { get; set; }
        GameObject gameObject { get; }
        bool enabled { get; set; }
    }
}
