/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;


namespace Arteranos.Core.Managed
{
    public interface IFavouriteable
    {
        void Favourite();
        void Unfavourite();
        bool IsFavourited {  get; }
        void UpdateLastSeen();
        DateTime LastSeen { get; }
    }
}