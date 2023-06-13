/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections.Generic;

using UnityEngine;

using Mirror;
using Arteranos.Core;
using Arteranos.Social;
using System;

namespace Arteranos.Avatar
{
    public class AvatarSubconscious : NetworkBehaviour
    {
        // ---------------------------------------------------------------
        #region Network

        // The own idea about the other users
        private readonly Dictionary<UserID, int> OwnSocialState = new();

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

        [Command]
        private void CmdUpdateReflectiveSocialState(UserID userID, int state)
        {
            IAvatarBrain mirrored = SearchUser(userID);

            if(mirrored != null)
            {
                // And, update the reflected state in the target user.
                mirrored.gameObject.GetComponent<AvatarSubconscious>()
                    .ReflectiveSocialState[Brain.UserID] = state;
            }
        }

        [Command]
        private void CmdTransmitGlobalUserID(GameObject receiverGO, UserID globalUserID)
        {
            if(globalUserID.ServerName != null) throw new ArgumentException("Not a global userID");

            receiverGO.GetComponent<AvatarSubconscious>().TargetReceiveGlobalUserID(
                gameObject,
                globalUserID);
        }

        [TargetRpc]
        private void TargetReceiveGlobalUserID(GameObject senderGO, UserID globalUserID)
        {
            if(!isOwned) throw new Exception("Not owner");

            IAvatarBrain sender = senderGO.GetComponent<AvatarBrain>();

            UserID supposedUserID = globalUserID.Derive();

            if(sender.UserID != supposedUserID) throw new Exception("Received global User ID goesn't match to the sender's -- possible MITM attack?");

            Brain.LogDebug($"{sender.Nickname}'s UserID was updated to global UserID {globalUserID}");

            if(sender.UserID != globalUserID) throw new Exception("Received global User ID goesn't match to the sender's -- possible MITM attack?");

            SettingsManager.Client.UpdateToGlobalUserID(globalUserID);
        }

        private IAvatarBrain SearchUser(UserID userID)
        {
            foreach(AvatarBrain brain in FindObjectsOfType<AvatarBrain>())
                if(brain.UserID == userID) return brain;

            return null;
        }

        private void SaveSocialState(IAvatarBrain receiver, UserID userID)
        {
            SettingsManager.Client.SaveSocialStates(userID,
                receiver?.Nickname ?? "<unknown>",
                OwnSocialState[userID]);
        }

        private void ReloadSocialState(UserID userID, int state)
        {
            OwnSocialState[userID] = state;

            CmdUpdateReflectiveSocialState(userID, state);

            Brain.UpdateSSEffects(SearchUser(userID), state);
        }

        private void UpdateSocialState(IAvatarBrain receiver, int state, bool set)
        {
            UserID userID = receiver.UserID;

            if(!OwnSocialState.ContainsKey(userID)) OwnSocialState[userID] = SocialState.None;

            if(set)
                OwnSocialState[userID] |= state;
            else
                OwnSocialState[userID] &= ~state;

            Brain.UpdateSSEffects(receiver, OwnSocialState[userID]);

            CmdUpdateReflectiveSocialState(userID, OwnSocialState[userID]);

            SaveSocialState(receiver, userID);
        }
        public void InitializeSocialStates()
        {
            // Clean slate
            OwnSocialState.Clear();

            // Copy users with the global UserIDs and the scoped UserIDs matching this server
            // Note, both current and logged-out users!
            IEnumerable<SocialListEntryJSON> q = SettingsManager.Client.GetFilteredSocialList(null);

            // But, derive the global UserIDs to the scoped UserIDs.
            foreach(SocialListEntryJSON item in q)
                ReloadSocialState(item.UserID.Derive(), item.state);
        }

        private void OnReflectiveStateUpdated(SyncIDictionary<UserID, int>.Operation op, UserID key, int item)
        {
            item = (item & SocialState.OWN_MASK) << SocialState.THEM_SHIFT;

            int old = OwnSocialState.TryGetValue(key, out int v) ? v : SocialState.None;

            OwnSocialState[key] = (old & SocialState.OWN_MASK) | item;

            IAvatarBrain receiver = SearchUser(key);

            // Maybe already gone?
            if(receiver != null) 
                Brain.UpdateSSEffects(receiver, OwnSocialState[key]);

            SaveSocialState(receiver, key);
        }

        #endregion
        // ---------------------------------------------------------------
        #region Interface

        public void AnnounceArrival(UserID userID)
        {
            userID = userID.Derive();
            int state = OwnSocialState.TryGetValue(userID, out int v) ? v : SocialState.None;

            ReloadSocialState(userID, state);
        }

        public void OfferFriendship(IAvatarBrain receiver, bool offering = true)
            => UpdateSocialState(receiver, SocialState.Own_Friend_offered, offering);

        public void BlockUser(IAvatarBrain receiver, bool blocking = true)
        {
            if(blocking)
                UpdateSocialState(receiver, SocialState.Own_Friend_offered, false);

            UpdateSocialState(receiver, SocialState.Own_Blocked, blocking);
        }

        public void AttemptFriendNegotiation(IAvatarBrain receiver)
        {
            bool result = IsMutualFriends(receiver);

            if(!result) return;

            // But, I have to take the first step....
            CmdTransmitGlobalUserID(receiver.gameObject, SettingsManager.Client.UserID);
        }

        public bool IsMutualFriends(IAvatarBrain receiver)
        {
            // "I love you, you love me, let us..." -- nope. Nope! NOPE!!
            // Not that imbecile pink dinosaur !
            int you = OwnSocialState.TryGetValue(receiver.UserID, out int v1) ? v1 : SocialState.None;

            bool result = SocialState.IsState(
                you, SocialState.Own_Friend_offered | SocialState.Them_Friend_offered);
            
            return result;
        }

        public int GetOwnState(IAvatarBrain receiver)
            => OwnSocialState.TryGetValue(receiver.UserID, out int v) ? v : SocialState.None;

        #endregion
        // ---------------------------------------------------------------
    }
}
