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
using Mirror;

namespace Arteranos.Core
{
    public enum SCMType
    {
        _Invalid = 0,
        SrvReportUserInfo,      // Server to client : Server User State
        ClnUpdateUserInfo,      // Client to server : Server User State
    }

    public static class ServerConfig
    {
        #region Interface

        public static void UpdateServerUserState(ServerUserState user)
        {
            if (NetworkStatus.GetOnlineLevel() == OnlineLevel.Offline)
                // Directly access the user database
                UpdateLocalUserState(null, user);
            else
            {
                // Sign, encrypt and transmit.
                ClientSettings.TransmitMessage(
                    user,
                    SettingsManager.CurrentServer.ServerPublicKey,
                    out CMSPacket p);

                // Use Network Behavior to contact the remote server
                XRControl.Me.PerformServerPacket(SCMType.ClnUpdateUserInfo, p);
            }
        }

        public static void QueryServerUserBase(Action<ServerUserState> callback)
        {
            if (NetworkStatus.GetOnlineLevel() == OnlineLevel.Offline)
                // Directly access the user database
                foreach (ServerUserState q in QueryLocalUserBase(null)) callback?.Invoke(q);
            else
            {
                if(!HandleCallbacks(callback, ref Callback_ServerUserState)) return;
                // Use Network Behavior to contact the remote server
                XRControl.Me.QueryServerPacket(SCMType.SrvReportUserInfo);
            }
        }

        #endregion
        // ---------------------------------------------------------------
        #region Server actions

        [Server]
        public static byte[] ServerPerformServerPacket(IAvatarBrain source, SCMType type, CMSPacket p)
        {
            byte[] expectedSignatureKey = source.UserID;

            switch (type)
            {
                case SCMType.ClnUpdateUserInfo:
                    ServerSettings.ReceiveMessage(p, ref expectedSignatureKey, out ServerUserState user);
                    UpdateLocalUserState(source, user);
                    break;
                default:
                    throw new NotImplementedException();
            }

            return expectedSignatureKey;
        }


        #endregion
        // ---------------------------------------------------------------
        #region Client actions

        [Client]
        public static void TargetDeliverServerPacket(SCMType type, CMSPacket packet)
        {
            switch (type)
            {
                case SCMType.SrvReportUserInfo:
                    ClientDeliverServerPacket(packet, ref Callback_ServerUserState);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        #endregion
        // ---------------------------------------------------------------
        #region Client return

        private static Action<ServerUserState> Callback_ServerUserState = null;

        private static bool HandleCallbacks<T>(Action<T> callback, ref Action<T> callbackStore)
        {
            if (callback == null)
            {
                callbackStore = null;
                return false;
            }

            // Requests on top of an ongoing requests are ignored.
            if (callbackStore != null) return false;

            callbackStore = callback;
            return true;
        }

        private static void ClientDeliverServerPacket<T>(CMSPacket packet, ref Action<T> callback)
        {
            try
            {
                byte[] serverPublicKey = SettingsManager.CurrentServer.ServerPublicKey;
                ClientSettings.ReceiveMessage(packet, ref serverPublicKey, out List<T> packets);

                if (packets.Count == 0) callback = null;
                foreach (T entry in packets) callback?.Invoke(entry);
            }
            catch (Exception)
            {
                // Ignore injected messages.
            }
        }


        #endregion
        // ---------------------------------------------------------------
        #region Server side processing

        public static void UpdateLocalUserState(IAvatarBrain source, ServerUserState user)
        {
            if (!Utils.IsAbleTo(source, UserCapabilities.CanAdminServerUsers, null)) return;

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

        public static IEnumerable<ServerUserState> QueryLocalUserBase(IAvatarBrain source)
        {
            if (!Utils.IsAbleTo(source, UserCapabilities.CanAdminServerUsers, null)) yield break;

            IEnumerable<ServerUserState> q = SettingsManager.ServerUsers.FindUsers(new());

            foreach(var user in q) yield return user;
        }

        #endregion
    }
}
