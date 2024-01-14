/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

#if UNITY_EDITOR

using Arteranos.Core;
using Arteranos.Social;
using Ipfs;
using System;
using UnityEngine;

namespace Arteranos.Avatar
{
    public class MockBrain : MonoBehaviour, IAvatarBrain
    {
        public string AvatarURL => "6394c1e69ef842b3a5112221";

        public uint NetID => 9999;

        public string Nickname { get => UserID; }

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

        public UserID UserID { get; set; } = null;

        public UserPrivacy UserPrivacy { get; set; } = new();

        public ulong UserState { get; set; } = 0;
        public string Address { get; set; } = string.Empty;
        public string DeviceID { get; set; } = string.Empty;

        public float AvatarHeight { get; set; } = 175;
        public string CurrentWorld { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public event Action<int> OnAppearanceStatusChanged;

        [SerializeField] private int m_NetMuteStatus = 0;

        private void Start()
        {
            if(!isOwned) AvatarHitBoxFactory.New(this);

            Crypto crypto = new();

            UserID = new(crypto.PublicKey, "TI-99 4a");
        }

        public void BlockUser(IAvatarBrain receiver, bool blocking = true)
        {

        }
        public void OfferFriendship(IAvatarBrain receiver, bool offering = true)
        {

        }
        public void LogDebug(object message)
        {

        }
        public void LogError(object message)
        {

        }
        public void LogWarning(object message)
        {

        }
        public void SetAppearanceStatusBit(int ASBit, bool set)
        {
            if(set)
                AppearanceStatus |= ASBit;
            else
                AppearanceStatus &= ~ASBit;
        }

        public void UpdateSSEffects(IAvatarBrain receiver, ulong state)
        {

        }

        public ulong GetOwnState(IAvatarBrain receiver) => 0;

        public void SendTextMessage(IAvatarBrain receiver, string text)
            => LogDebug($"To {receiver.Nickname}: {text}");
        public void ReceiveTextMessage(IAvatarBrain sender, string text) => throw new NotImplementedException();
        public void PerformEmote(string emoteName) => throw new NotImplementedException();

        public bool IsAbleTo(UserCapabilities cap, IAvatarBrain target)
        {
            throw new NotImplementedException();
        }

        public void QueryServerPacket(SCMType type)
        {
            throw new NotImplementedException();
        }

        public void PerformServerPacket(SCMType type, CMSPacket p)
        {
            throw new NotImplementedException();
        }

        public void ServerKickUser(string reason)
        {
            throw new NotImplementedException();
        }

        public void MakeWorkdToChange(Cid cid)
        {
            throw new NotImplementedException();
        }
    }
}

#endif
