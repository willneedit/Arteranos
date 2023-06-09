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
using System.Linq;
using Arteranos.Social;
using System;
using Arteranos.XR;

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

            NetworkIdentity nid = receiverGO.GetComponent<NetworkIdentity>();
            TargetReceiveGlobalUserID(
                nid.connectionToClient,
                gameObject,
                globalUserID);
        }

        [TargetRpc]
        private void TargetReceiveGlobalUserID(NetworkConnectionToClient _, GameObject receiverGO, UserID globalUserID) 
        {
            IAvatarBrain receiver = receiverGO.GetComponent<AvatarBrain>();

            UserID supposedUserID = globalUserID.Derive();

            if(receiver.UserID != supposedUserID) throw new Exception("Received global User ID goesn't match to the sender's -- possible MITM attack?");

            XRControl.Me.UpdateToGlobalUserID(receiver, globalUserID);
        }

        private IAvatarBrain SearchUser(UserID userID)
        {
            foreach(AvatarBrain brain in FindObjectsOfType<AvatarBrain>())
                if(brain.UserID == userID) return brain;

            return null;
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

            Brain.SaveSocialStates(receiver, OwnSocialState[userID]);
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
            => Brain.UpdateReflectiveSSEffects(SearchUser(key), item);

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
            => UpdateSocialState(receiver, SocialState.Friend_offered, offering);

        public void BlockUser(IAvatarBrain receiver, bool blocking = true)
        {
            if(blocking)
                UpdateSocialState(receiver, SocialState.Friend_offered, false);
            UpdateSocialState(receiver, SocialState.Blocked, blocking);
        }

        public void AttemptFriendNegotiation(IAvatarBrain receiver)
        {
            bool result = IsMutualFriends(receiver);

            if(!result) return;

            // But, I have to take the first step....
            CmdTransmitGlobalUserID(receiver.gameObject,
                SettingsManager.Client.UserID);
        }

        public bool IsMutualFriends(IAvatarBrain receiver)
        {
            // "I love you, you love me, let us..." -- nope. Nope! NOPE!!
            // Not that imbecile pink dinosaur !
            int you = OwnSocialState.TryGetValue(receiver.UserID, out int v1) ? v1 : SocialState.None;
            int? him = ReflectiveSocialState.TryGetValue(receiver.UserID, out int v2) ? v2 : null;

            if(!him.HasValue)
            {
                // Maybe he's not completely logged in. Hold off with the updating of the states.
                Brain.LogDebug($"Possible friendship? you={you}, him, I don't know yet.");
                return false;
            }

            bool result = SocialState.IsFriends(you, him ?? SocialState.None);
            bool wasMutual = (you & SocialState.Friend_bonded) != 0;

            Brain.LogDebug($"Possible friendship? you={you}, him={him} - result: {result}");

            if(wasMutual != result)
            {
                if(result)
                    you |= SocialState.Friend_bonded;
                else
                    you &= ~SocialState.Friend_bonded;

                Brain.LogDebug($"{receiver.Nickname}'s status is updated to {you}");

                OwnSocialState[receiver.UserID] = you;
                Brain.SaveSocialStates(receiver, you);
            }

            return result;
        }

        public int GetOwnState(IAvatarBrain receiver)
            => OwnSocialState.TryGetValue(receiver.UserID, out int v) ? v : SocialState.None;

        public int GetReflectiveState(IAvatarBrain receiver)
            => ReflectiveSocialState.TryGetValue(receiver.UserID, out int v) ? v : SocialState.None;

        #endregion
        // ---------------------------------------------------------------
    }
}
