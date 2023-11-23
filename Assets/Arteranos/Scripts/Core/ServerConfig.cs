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
        ClnUpdateUserInfo,      // Client to server : Server User State (ServerUserState)
        ClnKickUser             // Client to Server : Kick/Ban targeted user (KickPacket)
    }

    public struct KickPacket
    {
        public UserID UserID;
        public ServerUserState State;
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
                Client.TransmitMessage(
                    user,
                    SettingsManager.ActiveServerData.ServerPublicKey,
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
                XRControl.Me?.QueryServerPacket(SCMType.SrvReportUserInfo);
            }
        }

        public static void KickUser(KickPacket kp)
        {
            if (NetworkStatus.GetOnlineLevel() == OnlineLevel.Offline)
                // Uuh? Maybe for embedded AI's? 
                CommitLocalKickUser(null, kp);
            else
            {
                // Sign, encrypt and transmit.
                Client.TransmitMessage(
                    kp, 
                    SettingsManager.ActiveServerData.ServerPublicKey, 
                    out CMSPacket p);

                // Use Network Behavior to contact the remote server
                XRControl.Me.PerformServerPacket(SCMType.ClnKickUser, p);

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
                    Server.ReceiveMessage(p, ref expectedSignatureKey, out ServerUserState user);
                    UpdateLocalUserState(source, user);
                    break;
                case SCMType.ClnKickUser:
                    Server.ReceiveMessage(p, ref expectedSignatureKey, out KickPacket target);
                    CommitLocalKickUser(source, target);
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
                byte[] serverPublicKey = SettingsManager.ActiveServerData.ServerPublicKey;
                Client.ReceiveMessage(packet, ref serverPublicKey, out List<T> packets);

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

        public static void CommitLocalKickUser(IAvatarBrain source, KickPacket kickPacket)
        {

            ServerUserState toGo = kickPacket.State;
            IAvatarBrain target = NetworkStatus.GetOnlineUser(kickPacket.UserID);

            // Maybe it's already gone.
            if (target == null) return;

            // Fill up the banPacket's fields server-side
            if (toGo.userID != null) toGo.userID = target.UserID;
            if (toGo.address != null) toGo.address = target.Address;
            if (toGo.deviceUID != null) toGo.deviceUID = target.DeviceID;

            bool allowed = UserState.IsBanned(toGo.userState)
                ? Utils.IsAbleTo(source, UserCapabilities.CanBanUser, target)
                : Utils.IsAbleTo(source, UserCapabilities.CanKickUser, target);

            if (!allowed) return;

            if (UserState.IsBanned(toGo.userState))
            {
                SettingsManager.ServerUsers.AddUser(toGo);
                SettingsManager.ServerUsers.Save();
            }

            string reason = UserState.IsBanned(toGo.userState)
                ? "You've been banned from this server."
                : "You've been kicked from this server.";


            target.ServerKickUser(reason);
        }

        #endregion
    }
}
