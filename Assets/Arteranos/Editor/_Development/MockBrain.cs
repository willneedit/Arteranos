/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

#if UNITY_EDITOR

using Arteranos.Core;
using Arteranos.Core.Cryptography;
using Arteranos.Social;
using Ipfs;
using Ipfs.Cryptography.Proto;
using System;
using UnityEngine;

namespace Arteranos.Avatar
{
    public class MockBrain : MonoBehaviour, IAvatarBrain
    {
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
            bool haveFriends = true;

            // Not for the desired sphere of influence
            if(isFriend != haveFriends) return;

            touchy.SetAppearanceStatusBit(Avatar.AppearanceStatus.Bubbled, entered);
        }

        public IAvatarBody Body => GetComponent<IAvatarBody>();

        public UserID UserID { get; set; } = null;

        public UserPrivacy UserPrivacy { get; set; } = new();

        public ulong UserState { get; set; } = 0;
        public string Address { get; set; } = string.Empty;
        public string DeviceID { get; set; } = string.Empty;
        public PublicKey AgreePublicKey { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string UserIcon => throw new NotImplementedException();

        public event Action<int> OnAppearanceStatusChanged;

        [SerializeField] private int m_NetMuteStatus = 0;

        private void Start()
        {
            AvatarHitBoxFactory.New(this);

            UserID = new(null, "TI-99 4a");
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

        public ulong GetSocialStateTo(IAvatarBrain receiver) => 0;

        public void SendTextMessage(IAvatarBrain receiver, string text)
            => LogDebug($"To {receiver.Nickname}: {text}");
        public void ReceiveTextMessage(IAvatarBrain sender, string text) => throw new NotImplementedException();
        public void PerformEmote(string emoteName) => throw new NotImplementedException();

        public bool IsAbleTo(UserCapabilities cap, IAvatarBrain target)
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

        public void ReceiveCTCPacket(CTCPacketEnvelope envelope)
        {
            throw new NotImplementedException();
        }

        public void SendSocialState(IAvatarBrain receiver, ulong state)
        {
            throw new NotImplementedException();
        }
    }
}

#endif
