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

namespace Arteranos.Avatar
{
    public class AvatarSubconscious : NetworkBehaviour
    {
        private readonly SyncDictionary<UserID, int> OwnSocialState = new();

        private void Reset()
        {
            syncDirection = SyncDirection.ServerToClient;
            syncMode = SyncMode.Owner;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            // Initialize (and upload to the server) the filtered friend (and shit) list
            if(isOwned)
                InitializeSocialStates();
        }

        public override void OnStopClient() => base.OnStopClient();


        // TODO Mock, for the saved dictionary
        private readonly Dictionary<UserID, int> SavedSocialStates = new();


        [Command]
        private void CmdUploadSocialState(UserID userID, int state) => OwnSocialState[userID] = state;


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

        public int AskRelationsToMe(IAvatarBrain asked)
        {
            if(OwnSocialState.ContainsKey(asked.UserID))
                return OwnSocialState[asked.UserID];

            return SocialState.None;
        }

        /// <summary>
        /// Get the relations of the asking user to all the others around the server to him
        /// </summary>
        /// <param name="asker">The asking user</param>
        [Command]
        private void CmdAskRelationsToMe(GameObject askerGO)
        {
            IAvatarBrain asker = askerGO.GetComponent<IAvatarBrain>();
            NetworkIdentity nid = askerGO.GetComponent<NetworkIdentity>();

            foreach(var user in SettingsManager.Users)
                TargetTellRelationsToMe(nid.connectionToClient, user.gameObject, user.AskRelationsToMe(asker));
        }

        [TargetRpc]
        private void TargetTellRelationsToMe(
            NetworkConnectionToClient ncc, GameObject user, int state)
        {
            gameObject.GetComponent<AvatarSubconscious>().OnTellRelationsToMe(user, state);
        }

        /// <summary>
        /// The faint voice in your head telling you that the whole world is set out for you... :)
        /// </summary>
        /// <param name="user">The user who hates you or loves you</param>
        /// <param name="state">How he's really think of you</param>
        public void OnTellRelationsToMe(GameObject userGO, int state)
        {
            IAvatarBrain userBrain = userGO.GetComponent<IAvatarBrain>();
            IAvatarBrain brain = gameObject.GetComponent<IAvatarBrain>();

            Debug.Log($"[{brain.Nickname}] Got an updated relations entry for me - it's {state}.");

            // TODO retaliatory blocking
            // TODO distribute the markers on the alien avatars
        }


    }
}
