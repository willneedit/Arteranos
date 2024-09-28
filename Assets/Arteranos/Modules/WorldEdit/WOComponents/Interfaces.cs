/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

namespace Arteranos.WorldEdit.Components
{
    // Object is supposed to be clickable
    public interface IClickable
    {
        void GotClicked();
    }

    // Object is a 'meta' object. Visible im editor, but inactive out of the edit mode
    // e.g. a template for spawned objects or a teleport marker or respawn location
    public interface IMetaObject
    {

    }
}