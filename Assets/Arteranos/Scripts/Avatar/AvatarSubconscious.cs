/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Mirror;
using Arteranos.Core;
using System.Linq;
using Arteranos.Social;
using System;

namespace Arteranos.Avatar
{
    public class AvatarSubconscious : NetworkBehaviour
    {
        // ---------------------------------------------------------------
        #region Network

        // The own idea about the other users
        private readonly SyncDictionary<UserID, int> OwnSocialState = new();

        // The other user's ideas about you
        public readonly SyncDictionary<UserID, int> ReflectiveSocialState = new();

        private IAvatarBrain Brain = null;

        private void Reset()
        {
            syncDirection = SyncDirection.ServerToClient;
            syncMode = SyncMode.Owner;
        }

        private void Awake() => Brain = GetComponent<AvatarBrain>();

        public override void OnStartClient()
        {
            base.OnStartClient();
            if(isOwned)
                ReflectiveSocialState.Callback += OnReflectiveStateUpdated;

            // Initialize (and upload to the server) the filtered friend (and shit) list
            if(isOwned)
                InitializeSocialStates();
        }

        public override void OnStopClient()
        {
            ReflectiveSocialState.Callback -= OnReflectiveStateUpdated;

            base.OnStopClient();
        }


        // TODO Mock, for the saved dictionary
        private readonly Dictionary<UserID, int> SavedSocialStates = new();


        private void UpdateReflectiveSocialState(UserID userID, int state)
        {
            IEnumerable<IAvatarBrain> q =
                from user in SettingsManager.Users
                where user.UserID == userID
                select user;

            if(q.Count() > 0)
            {
                // And, update the reflected state in the target user.
                IAvatarBrain mirroredBrain = q.First();

                mirroredBrain.gameObject.GetComponent<AvatarSubconscious>()
                    .ReflectiveSocialState[Brain.UserID] = state;
            }
        }

        [Command]
        private void CmdUploadSocialState(UserID userID, int state)
        {
            OwnSocialState[userID] = state;

            UpdateReflectiveSocialState(userID, state);
        }


        [Command]
        private void CmdUpdateSocialState(UserID userID, int state, bool set)
        {
            if(!OwnSocialState.ContainsKey(userID)) OwnSocialState[userID] = SocialState.None;

            if(set)
                OwnSocialState[userID] |= state;
            else
                OwnSocialState[userID] &= ~state;

            UpdateReflectiveSocialState(userID, OwnSocialState[userID]);
        }

        public void InitializeSocialStates()
        {
            // Clean slate
            OwnSocialState.Clear();

            // Copy users with the global UserIDs and the scoped UserIDs matching this server
            var q = from state in SavedSocialStates
                    where state.Key.ServerName == null
                        || state.Key.ServerName == SettingsManager.CurrentServer.Name
                    select new
                    {
                        User = state.Key.Derive(SettingsManager.CurrentServer.Name),
                        State = state.Value
                    };

            // But, derive the global UserIDs to the scoped UserIDs.
            foreach(var item in q)
                CmdUploadSocialState(item.User, item.State);
        }

        private void OnReflectiveStateUpdated(SyncIDictionary<UserID, int>.Operation op, UserID key, int item)
        {
            Brain.LogDebug($"{key} feels about me: {item}");
        }

        #endregion
        // ---------------------------------------------------------------
        #region Interface


        public void OfferFriendship(IAvatarBrain receiver, bool offering = true)
        {
            CmdUpdateSocialState(receiver.UserID, SocialState.Friend_offered, offering);
        }


        public void BlockUser(IAvatarBrain receiver, bool blocking = true)
        {
            if(blocking)
                CmdUpdateSocialState(receiver.UserID, SocialState.Friend_offered, false);
            CmdUpdateSocialState(receiver.UserID, SocialState.Blocked, blocking);
        }

        #endregion
        // ---------------------------------------------------------------
    }
}
