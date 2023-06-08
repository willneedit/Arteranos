/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

#pragma warning disable IDE1006 // Because Unity's more relaxed naming convention

using Arteranos.Core;
using System;
using UnityEngine;

namespace Arteranos.Avatar
{
    public interface IAvatarBrain
    {
        string AvatarURL { get; }
        int ChatOwnID { get; }
        uint NetID { get; }
        string Nickname { get; }
        int AppearanceStatus { get; set; }
        bool isOwned { get; }
        IAvatarLoader Body { get; }
        GameObject gameObject { get; }
        UserID UserID { get; }

        event Action<string> OnAvatarChanged;
        event Action<int> OnAppearanceStatusChanged;

        void BlockUser(IAvatarBrain receiver, bool blocking = true);
        bool IsMutualFriends(IAvatarBrain receiver);
        void LogDebug(object message);
        void LogError(object message);
        void LogWarning(object message);
        void NotifyBubbleBreached(IAvatarBrain touchy, bool isFriend, bool entered);
        void OfferFriendship(IAvatarBrain receiver, bool offering = true);
        void SaveSocialStates(IAvatarBrain receiver, int state);
        void SetAppearanceStatusBit(int ASBit, bool set);
        void UpdateReflectiveSSEffects(IAvatarBrain receiver, int state);
        void UpdateSSEffects(IAvatarBrain receiver, int state);
        void UpdateToGlobalUserID(IAvatarBrain receiver, UserID globalUserID);
    }
}
