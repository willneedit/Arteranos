﻿/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;

namespace Arteranos.WorldEdit.Components
{
    // Object is supposed to be clickable
    public interface IClickable
    {
        // Object has been activated (a.k.a left-clicked, or with the trigger button)
        void GotClicked();

        void ServerGotClicked();
    }

    public interface IRigidBody
    {
        // Object has been grabbed (a.k.a right-clicked, or with the grab button)
        void GotGrabbed();

        // Object has been just released
        void GotReleased();

        // Object is movable by users at all
        bool IsMovable { get; }

        void ServerGotGrabbed();

        void ServerGotReleased();

        void ServerGotObjectHeld(Vector3 position, Quaternion rotation);
    }

    // Object is a 'meta' object. Visible im editor, but inactive out of the edit mode
    // e.g. a template for spawned objects or a teleport marker or respawn location
    public interface IMetaObject
    {

    }
}