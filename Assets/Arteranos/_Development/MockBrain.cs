/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

#if UNITY_EDITOR

using Arteranos.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arteranos.Avatar
{
    public class MockBrain : MonoBehaviour, IAvatarBrain
    {
        public string AvatarURL => "6394c1e69ef842b3a5112221";

        public int ChatOwnID => 9999;

        public uint NetID => 9999;

        public byte[] UserHash => new byte[] { 
            0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xaa, 0xbb, 0xcc, 0xdd, 0xee, 0xff,
            0xad, 0xea, 0xdb, 0xee, 0xab, 0xad, 0xf0, 0x0d, 0xca, 0xfe, 0xf0, 0x0d, 0xde, 0xad, 0xbe, 0xef
        };

        public string Nickname => "TI-99 4a";

        public int AppearanceStatus { 
            get => m_NetMuteStatus;
            set 
            {
                m_NetMuteStatus= value;
                OnAppearanceStatusChanged?.Invoke(m_NetMuteStatus);
            } 
        }

        public void NotifyBubbleBreached(IAvatarBrain touchy, bool isFriend, bool entered)
        {
            // TODO People management
            bool haveFriends = true;

            // Not for the desired sphere of influence
            if(isFriend != haveFriends) return;

            touchy.SetAppearanceStatusBit(Avatar.AppearanceStatus.Bubbled, entered);
        }

        public bool isOwned => false;

        public IAvatarLoader Body => GetComponent<IAvatarLoader>();

        public UserID UserID => new(UserHash, null);

        public byte[] PublicKey { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public event Action<string> OnAvatarChanged;
        public event Action<int> OnAppearanceStatusChanged;

        [SerializeField] private int m_NetMuteStatus = 0;

        private void Start()
        {
            if(!isOwned) AvatarHitBoxFactory.New(this);
        }

        public int AskRelationsToMe(IAvatarBrain asked) => throw new NotImplementedException();
        public void BlockUser(IAvatarBrain receiver, bool blocking = true) => throw new NotImplementedException();
        public void OfferFriendship(IAvatarBrain receiver, bool offering = true) => throw new NotImplementedException();
        public void LogDebug(object message) => throw new NotImplementedException();
        public void LogError(object message) => throw new NotImplementedException();
        public void LogWarning(object message) => throw new NotImplementedException();
        public void SetAppearanceStatusBit(int ASBit, bool set)
        {
            if(set)
                AppearanceStatus |= ASBit;
            else
                AppearanceStatus &= ~ASBit;
        }

        public void UpdateReflectiveSSEffects(IAvatarBrain receiver, int state)
        {

        }

        public void UpdateSSEffects(IAvatarBrain receiver, int state)
        {

        }

        public void SaveSocialStates(IAvatarBrain receiver, int state) => throw new NotImplementedException();
        public void UpdateToGlobalUserID(IAvatarBrain receiver, UserID globalUserID) => throw new NotImplementedException();
        public bool IsMutualFriends(IAvatarBrain receiver) => throw new NotImplementedException();
        public int GetReflectiveState(IAvatarBrain receiver) => throw new NotImplementedException();
        public int GetOwnState(IAvatarBrain receiver) => throw new NotImplementedException();
    }
}

#endif
