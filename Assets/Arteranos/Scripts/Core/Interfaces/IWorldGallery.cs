/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;

namespace Arteranos.Web
{
    public interface IWorldGallery
    {
        void DeleteWorld(string url);
        (string, string) RetrieveWorld(string url, bool cached = false);
        WorldMetaData RetrieveWorldMetaData(string url);
        bool StoreWorld(string url);
        void StoreWorldMetaData(string url, WorldMetaData worldMetaData);
    }
}
