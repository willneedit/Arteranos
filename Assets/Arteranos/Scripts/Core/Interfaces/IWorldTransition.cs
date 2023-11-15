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
    public struct WorldData
    {
        public string worldURL;
        public string name;
        public UserID authorID;
        public byte[] icon;
        public ServerPermissions permissions;
    }

    public interface IWorldTransition
    {
        //void InitiateTransition(string url, Action<Exception, Context> failureCallback = null, Action<Context> successCallback = null);

        public Task<(Exception, Context)> PreloadWorldDataAsync(string worldURL, bool forceReload = false);

        public bool IsWorldPreloaded(string worldURL);

        public Task<(Exception, WorldData)> GetWorldDataAsync(string worldURL);

        public Task<Exception> VisitWorldAsync(string worldURL, bool forceReload = false);

        public Task MoveToOfflineWorld();

        public Task MoveToOnlineWorld(string worldURL);
        Task EnterWorldAsync(string worldURL, bool forceReload = false);
        void EnterDownloadedWorld(string worldABF);
    }
}
