/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections.Generic;
using Arteranos.XR;
using Arteranos.Avatar;
using Arteranos.Services;
using Arteranos.Social;

namespace Arteranos.Core
{
    public static class ServerConfig
    {
        public static void UpdateServerUserState(ServerUserState user)
        {
            if (NetworkStatus.GetOnlineLevel() != OnlineLevel.Client)
                // Directly access the user database
                UpdateLocalUserState(user);
            else
                // Use Network Behavior to contact the remote server
                XRControl.Me.UpdateServerUserState(user);
        }

        public static void QueryServerUserBase(Action<ServerUserState> callback)
        {
            if (NetworkStatus.GetOnlineLevel() != OnlineLevel.Client)
                // Directly access the user database
                foreach (ServerUserState q in QueryLocalUserBase()) callback?.Invoke(q);
            else
                // Use Network Behavior to contact the remote server
                XRControl.Me.QueryServerUserBase(callback);
        }

        public static void UpdateLocalUserState(ServerUserState user)
        {
            if (!Utils.IsAbleTo(UserCapabilities.CanAdminServerUsers, null)) return;

            ServerUserBase sub = SettingsManager.ServerUsers;

            sub.RemoveUsers(user);
            sub.AddUser(user);
            sub.Save();

            // If the targeted user is approachable, immediately update the user's state.
            if (user.userID != null)
            {
                foreach (IAvatarBrain onlineUser in NetworkStatus.GetOnlineUsers())
                    if (user.userID == onlineUser.UserID)
                        onlineUser.UserState = user.userState;
            }
        }

        public static IEnumerable<ServerUserState> QueryLocalUserBase()
        {
            if (!Utils.IsAbleTo(UserCapabilities.CanAdminServerUsers, null)) yield break;

            IEnumerable<ServerUserState> q = SettingsManager.ServerUsers.FindUsers(new());

            foreach(var user in q) yield return user;
        }
    }
}
