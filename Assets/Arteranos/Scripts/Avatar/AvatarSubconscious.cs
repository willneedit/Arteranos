/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;

using Arteranos.Core;
using Arteranos.Social;
using System;
using System.Linq;
using Arteranos.XR;
using Arteranos.Core.Cryptography;
using System.Text;
using Ipfs.Core.Cryptography.Proto;
using System.Collections;
using System.Collections.Generic;
using Mirror;

namespace Arteranos.Avatar
{
    public class AvatarSubconscious : NetworkBehaviour
    {
        #region Start & Stop

        // The own ideas about the other users, and vice versa
        private readonly Dictionary<UserID, ulong> SocialMemory = new();

        private IAvatarBrain Brain = null;

        // private bool isOwned => Brain.isOwned;

        private void Awake() => Brain = GetComponent<IAvatarBrain>();

        // Formerly NetworkBehavior, but now it's slaved to the Brain.
        // Heh, Id, I ... but no superego :)
        public void OnStartClientSlaved()
        {
            IEnumerator AnnounceArrival()
            {
                // I'm a remote avatar, and we haven't seen the local user yet.
                while (XRControl.Me == null)
                    yield return new WaitForSeconds(1);

                XRControl.Me.LogDebug($"{Brain.UserID} announcing its arrival");

                // Now we're talking!
                XRControl.Me.gameObject.
                    GetComponent<AvatarSubconscious>().AnnounceArrival(Brain.UserID);
            }

            if(isOwned)
                InitializeSocialStates();           // Initialize the filtered friend (and shit) list
            else
                StartCoroutine(AnnounceArrival());  // Tell the local user that I'm here
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
            string text;
            PublicKey signerPublicKey;

            try
            {
                Client.ReceiveMessage(p, out byte[] data, out signerPublicKey);
                text = Encoding.UTF8.GetString(data);
            }
            catch { return; } // Discard malformed messages

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
            // Can happen -- timing problem with users just logging in or out
            if (!senderGO)
            {
                Debug.LogWarning("Discarding reflective social state of vanished user");
                return;
            }

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

            // But, derive the global UserIDs to the scoped UserIDs.
            foreach (KeyValuePair<UserID, ulong> item in SettingsManager.Client.GetSocialList(null))
                ReloadSocialState(item.Key, item.Value);
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
