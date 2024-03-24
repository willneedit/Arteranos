using Arteranos.Services;
using Arteranos.UI;
using Arteranos.Web;
using Arteranos.XR;
using Ipfs;
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/*
* Copyright (c) 2023, willneedit
* 
* Licensed by the Mozilla Public License 2.0,
* residing in the LICENSE.md file in the project's root directory.
*/

namespace Arteranos.Core.Operations
{
    [Obsolete("Soon to be retired")]
    public static class WorldTransition
    {

        /// <summary>
        /// Called from the client, either have the transition locally, or incite the
        /// server to do the transition.
        /// </summary>
        /// <param name="WorldCid"></param>
        /// 
        /// <returns>Task completed, or the server has been notified</returns>
        public static async Task EnterWorldAsync(Cid WorldCid)
        {
            WorldInfo wi = WorldInfo.DBLookup(WorldCid);

            await EnterWIAsync(wi);
        }

        public static async Task EnterWIAsync(WorldInfo wi)
        {
            await Task.Delay(1000);

            // Pawn it off to the network message delivery service
            SettingsManager.EmitToServerCTSPacket(new CTSPWorldChangeAnnouncement()
            {
                WorldInfo = wi?.Strip(),
            });
        }
    }
}