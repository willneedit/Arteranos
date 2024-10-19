/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;

namespace Arteranos.Services
{
    /// <summary>
    /// Helper interface to find the 'real' object behind the network object.
    /// </summary>
    public interface IEnclosingObject
    {
        GameObject EnclosedObject { get; }
        bool? IsOnServer { get; }

        void ChangeAuthority(GameObject targetGO, bool auth);
    }

    public interface IEnclosedObject
    {
        GameObject EnclosingObject { get; }
        bool? IsOnServer { get; }
    }
}