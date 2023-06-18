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
using System.Linq;
using Arteranos.XR;

namespace Arteranos.Avatar
{
    public class AvatarSubconscious : NetworkBehaviour
    {
        public const int READY_USERID   = (1 << 0);
        public const int READY_CRYPTO   = (1 << 1);
        public const int READY_ME       = (1 << 2);

        // ---------------------------------------------------------------
        #region Start & Stop

        public int ReadyState 
        {
            get => m_ReadyState;
            set
            {
                int old = m_ReadyState;
                m_ReadyState |= value; // NOTE: Intentional. Only for latches.
                if (old != m_ReadyState) ReadyStateChanged?.Invoke(old, m_ReadyState);
            }
        }

        public event Action<int, int> ReadyStateChanged;

        // The own ideas about the other users, and vice versa
        private readonly Dictionary<UserID, int> SocialMemory = new();

        private IAvatarBrain Brain = null;
        private Crypto crypto = null;

        private int m_ReadyState = 0;

        private void Reset()
        {
            syncDirection = SyncDirection.ServerToClient;
            syncMode = SyncMode.Observers;
        }

        private void Awake() => Brain = GetComponent<IAvatarBrain>();

        public override void OnStartClient()
        {
            base.OnStartClient();

            ReadyStateChanged += OnReadyStateChanged;

            // Initialize the crypto module and distribute the public key
            crypto = new();

            Brain.PublicKey = crypto.PublicKey;

            // Initialize the filtered friend (and shit) list
            if(isOwned)
                InitializeSocialStates();
        }

        public override void OnStopClient()
        {
            crypto.Dispose();

            ReadyStateChanged -= OnReadyStateChanged;

            base.OnStopClient();
        }

        private void Update()
        {
            if((ReadyState & READY_ME) == 0 && XRControl.Me != null)
                ReadyState = READY_ME;
        }


        private void OnReadyStateChanged(int oldval, int newval)
        {
            // Brain.LogDebug($"Ready state changed from {oldval} to {newval}");

            // Newly online user, do the neccessary handshake, once the userID and the public key
            // is available.
            int r1 = READY_CRYPTO|READY_USERID|READY_ME;
            if((newval & r1) == r1 && !isOwned)
            {
                XRControl.Me.gameObject.GetComponent<AvatarSubconscious>().
                    AnnounceArrival(Brain.UserID);
            }
        }


        #endregion
        // ---------------------------------------------------------------
        #region Network

        private static IAvatarBrain SearchUser(UserID userID)
        {
            foreach(AvatarBrain brain in FindObjectsOfType<AvatarBrain>())
                if(brain.UserID == userID) return brain;

            return null;
        }

        private static AvatarSubconscious GetSC(GameObject GO)
            => GO.GetComponent<AvatarSubconscious>();

        // ---------------------------------------------------------------
        #region Text message transmission

        private void TransmitTextMessage(GameObject receiverGO, string text)
        {
            crypto.Encrypt(text, receiverGO.GetComponent<IAvatarBrain>().PublicKey, out CryptPacket p);
            CmdTransmitTextMessage(receiverGO, p);
        }

        [Command]
        private void CmdTransmitTextMessage(GameObject receiverGO, CryptPacket p) 
            => GetSC(receiverGO).TargetReceiveTextMessage(gameObject, p);

        [TargetRpc]
        private void TargetReceiveTextMessage(GameObject senderGO, CryptPacket p)
        {
            crypto.Decrypt(p, out string text);
            ReceiveTextMessage(senderGO, text);
        }

        private void ReceiveTextMessage(GameObject senderGO, string text)
        {
            if(!isOwned) throw new Exception("Not owner");

            IAvatarBrain sender = senderGO.GetComponent<IAvatarBrain>();

            Brain.LogDebug($"Message from {sender.Nickname}: {text}");

            // TODO pass on to the higher level of consciousness.
        }

        #endregion
        // ---------------------------------------------------------------
        #region Reflectice Social State distribution

        [Command]
        private void CmdUpdateReflectiveSocialState(UserID userID, int state)
        {
            GameObject receiverGO = SearchUser(userID)?.gameObject;

            // And, update the reflected state in the target user.
            if(receiverGO != null)
                GetSC(receiverGO).TargetReceiveReflectiveSocialState(gameObject, state);
        }

        [TargetRpc]
        private void TargetReceiveReflectiveSocialState(GameObject senderGO, int state)
        {
            if(!isOwned) throw new Exception("Not owner");

            IAvatarBrain sender = senderGO.GetComponent<IAvatarBrain>();

            UserID key = sender.UserID;

            state = (state & SocialState.OWN_MASK) << SocialState.THEM_SHIFT;
            int old = SocialMemory.TryGetValue(key, out int v) ? v : SocialState.None;

            SocialMemory[key] = (old & SocialState.OWN_MASK) | state;
            
            Brain.UpdateSSEffects(sender, SocialMemory[key]);

            SaveSocialState(sender, key);

        }
        #endregion
        // ---------------------------------------------------------------
        #region Global UserID transmission
        private void TransmitGlobalUserID(GameObject receiverGO, UserID globalUserID)
        {
            if(globalUserID.ServerName != null) throw new ArgumentException("Not a global userID");
            crypto.Encrypt(globalUserID,
                receiverGO.GetComponent<IAvatarBrain>().PublicKey,
                out CryptPacket p);
            CmdTransmitGlobalUserID(receiverGO, p);
        }

        [Command]
        private void CmdTransmitGlobalUserID(GameObject receiverGO, CryptPacket p) 
            => GetSC(receiverGO).TargetReceiveGlobalUserID(gameObject, p);

        [TargetRpc]
        private void TargetReceiveGlobalUserID(GameObject senderGO, CryptPacket p)
        {
            crypto.Decrypt(p, out UserID globalUserID);
            ReceiveGlobalUserID(senderGO, globalUserID);
        }

        private void ReceiveGlobalUserID(GameObject senderGO, UserID globalUserID)
        {
            if(!isOwned) throw new Exception("Not owner");

            IAvatarBrain sender = senderGO.GetComponent<IAvatarBrain>();
            if(sender.UserID != globalUserID.Derive()) throw new Exception("Received global User ID goesn't match to the sender's -- possible MITM attack?");
            Brain.LogDebug($"{sender.Nickname}'s UserID was updated to global UserID {globalUserID}");
            SettingsManager.Client.UpdateToGlobalUserID(globalUserID);
        }
        #endregion
        // ---------------------------------------------------------------
        #endregion
        // ---------------------------------------------------------------
        #region Social State handling
        private void SaveSocialState(IAvatarBrain receiver, UserID userID)
        {
            SettingsManager.Client.SaveSocialStates(userID,
                receiver?.Nickname ?? "<unknown>",
                SocialMemory[userID]);
        }

        private void ReloadSocialState(UserID userID, int state)
        {
            SocialMemory[userID] = state;

            CmdUpdateReflectiveSocialState(userID, state);

            Brain.UpdateSSEffects(SearchUser(userID), state);
        }

        private void UpdateSocialState(IAvatarBrain receiver, int state, bool set)
        {
            UserID userID = receiver.UserID;

            if(!SocialMemory.ContainsKey(userID)) SocialMemory[userID] = SocialState.None;

            if(set)
                SocialMemory[userID] |= state;
            else
                SocialMemory[userID] &= ~state;

            Brain.UpdateSSEffects(receiver, SocialMemory[userID]);

            CmdUpdateReflectiveSocialState(userID, SocialMemory[userID]);

            SaveSocialState(receiver, userID);
        }
        public void InitializeSocialStates()
        {
            // Clean slate
            SocialMemory.Clear();

            // Copy users with the global UserIDs and the scoped UserIDs matching this server
            // Note, both current and logged-out users!
            IEnumerable<SocialListEntryJSON> q = SettingsManager.Client.GetFilteredSocialList(null);

            // But, derive the global UserIDs to the scoped UserIDs.
            foreach(SocialListEntryJSON item in q)
                ReloadSocialState(item.UserID.Derive(), item.state);
        }

        #endregion
        // ---------------------------------------------------------------
        #region Interface

        public void AnnounceArrival(UserID userID)
        {
            userID = userID.Derive();
            int state = SocialMemory.TryGetValue(userID, out int v) ? v : SocialState.None;

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

        public void SendTextMessage(IAvatarBrain receiver, string text)
            => TransmitTextMessage(receiver.gameObject, text);

        public void AttemptFriendNegotiation(IAvatarBrain receiver)
        {
            bool result = IsMutualFriends(receiver);

            if(!result) return;

            // But, I have to take the first step....
            TransmitGlobalUserID(receiver.gameObject, SettingsManager.Client.UserID);
        }

        public bool IsMutualFriends(IAvatarBrain receiver)
        {
            // "I love you, you love me, let us..." -- nope. Nope! NOPE!!
            // Not that imbecile pink dinosaur !
            int you = SocialMemory.TryGetValue(receiver.UserID, out int v1) ? v1 : SocialState.None;

            bool result = SocialState.IsState(
                you, SocialState.Own_Friend_offered | SocialState.Them_Friend_offered);
            
            return result;
        }

        public int GetOwnState(IAvatarBrain receiver)
            => SocialMemory.TryGetValue(receiver.UserID, out int v) ? v : SocialState.None;

        #endregion
        // ---------------------------------------------------------------
    }
}
