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

namespace Arteranos.Avatar
{
    public class AvatarSubconscious : MonoBehaviour
    {
        #region Start & Stop

        // The own ideas about the other users, and vice versa
        private readonly Dictionary<UserID, ulong> SocialMemory = new();

        private IAvatarBrain Brain = null;

        private bool isOwned => Brain.isOwned;

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
        #region Reflectice Social State distribution

        private void CmdUpdateReflectiveSocialState(UserID userID, ulong state)
        {
            GameObject receiverGO = SearchUser(userID)?.gameObject;

            // And, update the reflected state in the target user.
            if(receiverGO != null)
                GetSC(receiverGO).TargetReceiveReflectiveSocialState(gameObject, state);
        }

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

        public ulong GetOwnState(UserID userID) 
            => SocialMemory.TryGetValue(userID, out ulong v) ? v : SocialState.None;

        #endregion
        // ---------------------------------------------------------------
    }
}
