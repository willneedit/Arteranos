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
        Task<(string, ServerMetadataJSON)> DownloadServerMetadataAsync(string url, int timeout = 20);
        void DownloadServerMetadataAsync(string url, Action<string, ServerMetadataJSON> callback, int timeout = 20);
        ServerJSON RetrieveServerSettings(string url);
        void StoreServerSettings(string url, ServerJSON serverSettings);
    }
}
