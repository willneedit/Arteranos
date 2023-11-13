/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

#pragma warning disable IDE1006 // Because Unity's more relaxed naming convention

using Arteranos.Core;
using Arteranos.Social;
using System;
using UnityEngine;

namespace Arteranos.Avatar
{
    public interface IAvatarBrain
    {
        string AvatarURL { get; }
        uint NetID { get; }
        string Nickname { get; }
        int AppearanceStatus { get; set; }
        bool isOwned { get; }
        IAvatarLoader Body { get; }
        GameObject gameObject { get; }
        Transform transform { get; }
        UserID UserID { get; set; }
        UserPrivacy UserPrivacy { get; }
        ulong UserState { get; set; }
        string Address { get; set; }
        string DeviceID { get; set; }
        float AvatarHeight { get; }
        string CurrentWorld { get; set; }

        event Action<int> OnAppearanceStatusChanged;

        void BlockUser(IAvatarBrain receiver, bool blocking = true);
        ulong GetOwnState(IAvatarBrain receiver);
        bool IsAbleTo(UserCapabilities cap, IAvatarBrain target);
        void LogDebug(object message);
        void LogError(object message);
        void LogWarning(object message);
        void NotifyBubbleBreached(IAvatarBrain touchy, bool isFriend, bool entered);
        void OfferFriendship(IAvatarBrain receiver, bool offering = true);
        void PerformEmote(string emoteName);
        void QueryServerPacket(SCMType type);
        void ReceiveTextMessage(IAvatarBrain sender, string text);
        void SendTextMessage(IAvatarBrain receiver, string text);
        void SetAppearanceStatusBit(int ASBit, bool set);
        void PerformServerPacket(SCMType type, CMSPacket p);
        void UpdateSSEffects(IAvatarBrain receiver, ulong state);
        void ServerKickUser(string reason);
    }
}
