/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using System;
using System.Threading.Tasks;

namespace Arteranos.Web
{
    public interface IServerGallery
    {
        void DeleteServerSettings(string url);
        ServerDescription? RetrieveServerSettings(string url);
        void StoreServerSettings(string url, ServerDescription onlineData);
    }
}
