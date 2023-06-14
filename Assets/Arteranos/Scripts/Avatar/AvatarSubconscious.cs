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
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Arteranos.Avatar
{
    public class AvatarSubconscious : NetworkBehaviour
    {
        // ---------------------------------------------------------------
        #region Start & Stop

        // The own idea about the other users
        private readonly Dictionary<UserID, int> SocialMemory = new();

        private IAvatarBrain Brain = null;

        private void Reset()
        {
            syncDirection = SyncDirection.ServerToClient;
            syncMode = SyncMode.Owner;
        }

        private void Awake() => Brain = GetComponent<IAvatarBrain>();

        public override void OnStartClient()
        {
            base.OnStartClient();

            // Initialize (and upload to the server) the filtered friend (and shit) list
            if(isOwned)
                InitializeSocialStates();
        }

        public override void OnStopClient() => base.OnStopClient();

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


        // TODO Surely I need the full-fledged DH key exchange here, because a
        // server administrator could be listening into the messages with the receiver's
        // global UserID.
        private byte[] Encode(string text, UserID userID)
        {
            byte[] encoded = null;

            using(MemoryStream memoryStream = new())
            {
                using(Aes aes = Aes.Create())
                {
                    aes.Key = userID.Hash;
                    byte[] iv = aes.IV;
                    memoryStream.Write(iv, 0, iv.Length);

                    using CryptoStream cryptoStream = new(
                        memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
                    using StreamWriter writer = new(cryptoStream);
                    writer.WriteLine(text);
                }

                encoded = new byte[memoryStream.Length];
                memoryStream.Read(encoded, 0, (int) memoryStream.Length);
            }

            return encoded;
        }

        private async Task<string> Decode(byte[] encoded, UserID userID)
        {
            string text = null;

            try
            {
                using MemoryStream memoryStream = new(encoded);

                using Aes aes = Aes.Create();

                byte[] iv = new byte[aes.IV.Length];
                if(encoded.Length < iv.Length)
                    throw new InvalidOperationException("Truncated ciphertext");

                memoryStream.Read(iv, 0, iv.Length);

                using CryptoStream cryptoStream = new(
                    memoryStream, aes.CreateDecryptor(userID.Hash, iv), CryptoStreamMode.Read);
                using StreamReader reader = new(cryptoStream);
                text = await reader.ReadToEndAsync();
            }
            catch(Exception e)
            {
                Brain.LogError(e.Message);
                text = string.Empty;
            }

            return text;
        }


        [Command]
        private void CmdSendTextMessage(UserID userID, string text)
        {
            GameObject receiverGO = SearchUser(userID)?.gameObject;

            if(receiverGO != null)
                GetSC(receiverGO).TargetReceiveTextMessage(gameObject, Encode(text, userID));
        }

        [TargetRpc]
        private void TargetReceiveTextMessage(GameObject senderGO, byte[] encoded)
        {
            if(!isOwned) throw new Exception("Not owner");

            IAvatarBrain sender = senderGO.GetComponent<IAvatarBrain>();

            ReceiveTextMessage(sender, encoded);

        }

        private async void ReceiveTextMessage(IAvatarBrain sender, byte[] encoded)
        {
            // Use the global UserID. It's only shared with the sender if they're friends.
            string text = await Decode(encoded, SettingsManager.Client.UserID);

            // TODO pass on to the higher level of consciousness.
        }

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

        [Command]
        private void CmdTransmitGlobalUserID(GameObject receiverGO, UserID globalUserID)
        {
            if(globalUserID.ServerName != null) throw new ArgumentException("Not a global userID");

            GetSC(receiverGO).TargetReceiveGlobalUserID(
                gameObject,
                globalUserID);
        }

        [TargetRpc]
        private void TargetReceiveGlobalUserID(GameObject senderGO, UserID globalUserID)
        {
            if(!isOwned) throw new Exception("Not owner");

            IAvatarBrain sender = senderGO.GetComponent<IAvatarBrain>();

            UserID supposedUserID = globalUserID.Derive();

            if(sender.UserID != supposedUserID) throw new Exception("Received global User ID goesn't match to the sender's -- possible MITM attack?");

            Brain.LogDebug($"{sender.Nickname}'s UserID was updated to global UserID {globalUserID}");

            if(sender.UserID != globalUserID) throw new Exception("Received global User ID goesn't match to the sender's -- possible MITM attack?");

            SettingsManager.Client.UpdateToGlobalUserID(globalUserID);
        }

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
