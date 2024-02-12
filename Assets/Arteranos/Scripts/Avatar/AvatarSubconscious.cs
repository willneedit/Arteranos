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
using Arteranos.Core.Cryptography;
using System.Text;
using Ipfs.Core.Cryptography.Proto;

namespace Arteranos.Avatar
{
    public class AvatarSubconscious : NetworkBehaviour
    {
        public const int READY_USERID   = (1 <<  0);
        public const int READY_CRYPTO   = (1 <<  1);
        public const int READY_ME       = (1 <<  2);
        public const int READY_ANNOUNCE = (1 << 24);
        public const int READY_COMPLETE = (1 << 30);

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
        private readonly Dictionary<UserID, ulong> SocialMemory = new();

        private IAvatarBrain Brain = null;

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

            // Initialize the filtered friend (and shit) list
            if(isOwned)
                InitializeSocialStates();
        }

        public override void OnStopClient()
        {
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
            Brain.LogDebug($"Ready state changed from {oldval} to {newval}");

            // Newly online user, do the neccessary handshake, once the userID and the public key
            // is available.
            int r1 = READY_CRYPTO|READY_USERID|READY_ME;
            if((newval & (r1|READY_ANNOUNCE)) == r1 && !isOwned)
            {
                ReadyState = READY_ANNOUNCE;
                XRControl.Me.gameObject.
                    GetComponent<AvatarSubconscious>().AnnounceArrival(Brain.UserID);
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
            IAvatarBrain receiverBrain = receiverGO.GetComponent<IAvatarBrain>();
            byte[] data = Encoding.UTF8.GetBytes(text);
            Client.TransmitMessage(data, receiverBrain.AgreePublicKey, out CMSPacket p);
            CmdTransmitTextMessage(receiverGO, p);
        }

        [Command]
        private void CmdTransmitTextMessage(GameObject receiverGO, CMSPacket p) 
            => GetSC(receiverGO).TargetReceiveTextMessage(gameObject, p);

        [TargetRpc]
        private void TargetReceiveTextMessage(GameObject senderGO, CMSPacket p)
        {
            Client.ReceiveMessage(p, out byte[] data, out PublicKey signerPublicKey);
            string text = Encoding.UTF8.GetString(data);

            // Already gone.
            if (senderGO == null) return;

            // Ignore injected messages
            IAvatarBrain senderBrain = senderGO.GetComponent<IAvatarBrain>();
            if(senderBrain.UserID.SignPublicKey != signerPublicKey)
                return;

            ReceiveTextMessage(senderGO, text);
        }

        private void ReceiveTextMessage(GameObject senderGO, string text)
        {
            if(!isOwned) throw new Exception("Not owner");

            IAvatarBrain sender = senderGO.GetComponent<IAvatarBrain>();

            Brain.LogDebug($"Message from {sender.Nickname}: {text}");

            // "Well... Errr... You've blocked that user." -- Discord, paraphrased
            if(SocialState.IsSomehowBlocked(GetOwnState(sender.UserID)))
                return;

            Brain.ReceiveTextMessage(sender, text);
        }

        #endregion
        // ---------------------------------------------------------------
        #region Reflectice Social State distribution

        [Command]
        private void CmdUpdateReflectiveSocialState(UserID userID, ulong state)
        {
            GameObject receiverGO = SearchUser(userID)?.gameObject;

            // And, update the reflected state in the target user.
            if(receiverGO != null)
                GetSC(receiverGO).TargetReceiveReflectiveSocialState(gameObject, state);
        }

        [TargetRpc]
        private void TargetReceiveReflectiveSocialState(GameObject senderGO, ulong state)
        {
            if(!isOwned) throw new Exception("Not owner");

            IAvatarBrain sender = senderGO.GetComponent<IAvatarBrain>();

            UserID key = sender.UserID;

            SocialMemory[key] = SocialState.ReflectSocialState(state, GetOwnState(key));

            Brain.UpdateSSEffects(sender, SocialMemory[key]);

            SaveSocialState(key);

        }

        #endregion
        // ---------------------------------------------------------------
        #endregion
        // ---------------------------------------------------------------
        #region Social State handling

        private void SaveSocialState(UserID userID) 
            => SettingsManager.Client.SaveSocialStates(userID, SocialMemory[userID]);

        private void ReloadSocialState(UserID userID, ulong state)
        {
            SocialMemory[userID] = state;

            CmdUpdateReflectiveSocialState(userID, state);

            Brain.UpdateSSEffects(SearchUser(userID), state);
        }

        private void UpdateSocialState(IAvatarBrain receiver, ulong state)
        {
            UserID userID = receiver.UserID;

            SocialMemory[userID] = state;

            Brain.UpdateSSEffects(receiver, SocialMemory[userID]);

            CmdUpdateReflectiveSocialState(userID, SocialMemory[userID]);

            SaveSocialState(userID);
        }
        public void InitializeSocialStates()
        {
            // Clean slate
            SocialMemory.Clear();

            // Copy users with the global UserIDs and the scoped UserIDs matching this server
            // Note, both current and logged-out users!
            IEnumerable<SocialListEntryJSON> q = SettingsManager.Client.GetSocialList(null);

            // But, derive the global UserIDs to the scoped UserIDs.
            foreach(SocialListEntryJSON item in q)
                if(item.UserID.SignPublicKey != null)
                    ReloadSocialState(item.UserID, item.State);
        }

        #endregion
        // ---------------------------------------------------------------
        #region Interface

        public void AnnounceArrival(UserID userID) 
            => ReloadSocialState(userID, GetOwnState(userID));

        public void OfferFriendship(IAvatarBrain receiver, bool offering = true)
        {
            ulong state = GetOwnState(receiver.UserID);
            SocialState.SetFriendState(ref state, offering);

            UpdateSocialState(receiver, state);
        }

        public void BlockUser(IAvatarBrain receiver, bool blocking = true)
        {
            ulong state = GetOwnState(receiver.UserID);
            if(blocking)
                SocialState.SetFriendState(ref state, false);

            SocialState.SetBlockState(ref state, blocking);
            UpdateSocialState(receiver, state);
        }

        public void SendTextMessage(IAvatarBrain receiver, string text)
            => TransmitTextMessage(receiver.gameObject, text);

        public ulong GetOwnState(UserID userID) 
            => SocialMemory.TryGetValue(userID, out ulong v) ? v : SocialState.None;

        #endregion
        // ---------------------------------------------------------------
    }
}
