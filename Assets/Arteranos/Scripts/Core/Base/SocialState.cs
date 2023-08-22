/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Avatar;
using Arteranos.XR;

namespace Arteranos.Social
{
    // -------------------------------------------------------------------
    #region Social & User constants
    public static class SocialState
    {
        public const int None = 0;

        // You offered your friendship to the targeted user.
        private const int Own_Friend_requested   = (1 << 0);

        // You blocked the targeted user.
        private const int Own_Blocked            = (1 << 4);

        private const int THEM_SHIFT             = 16;

        private const int OWN_MASK               = (1 << THEM_SHIFT) - 1;

        // The targeted user offered his frienship to you.
        private const int Them_Friend_requested  = Own_Friend_requested << THEM_SHIFT;

        // The targeted user blocked you.
        private const int Them_Blocked           = Own_Blocked << THEM_SHIFT;


        public static void Set(ref int field, int bits, bool desired)
        {
            if(desired)
                field |= bits;
            else
                field &= ~bits;
        }

        private static bool IsAll(int you, int stateBit)
            => ((you & stateBit) == stateBit);

        private static bool IsAny(int you, int stateBit)
            => ((you & stateBit) != 0);

        private static bool IsAll(IAvatarBrain target, int stateBit) 
            => IsAll(XRControl.Me?.GetOwnState(target) ?? None, stateBit);

        //private static bool IsAny(IAvatarBrain target, int stateBit)
        //    => IsAny(XRControl.Me?.GetOwnState(target) ?? None, stateBit);

        public static int ReflectSocialState(int other, int me)
            => (me & OWN_MASK) | (other & OWN_MASK) << THEM_SHIFT;

        public static bool IsFriends(IAvatarBrain target)
            => IsAll(target, Own_Friend_requested | Them_Friend_requested);

        public static bool IsFriends(int state)
            => IsAll(state, Own_Friend_requested | Them_Friend_requested);

        public static bool IsFriendOffered(IAvatarBrain target)
            => IsAll(target, Them_Friend_requested);

        public static bool IsFriendOffered(int state)
            => IsAll(state, Them_Friend_requested);

        public static bool IsFriendRequested(IAvatarBrain target)
            => IsAll(target, Own_Friend_requested);

        public static bool IsFriendRequested(int state)
            => IsAll(state, Own_Friend_requested);

        public static bool IsBlocked(IAvatarBrain target)
            => IsAll(target, Own_Blocked);

        public static bool IsBlocked(int state)
            => IsAll(state, Own_Blocked);

        public static bool IsBeingBlocked(IAvatarBrain target)
            => IsAll(target, Them_Blocked);

        public static bool IsBeingBlocked(int state)
            => IsAll(state, Them_Blocked);

        public static bool IsSomehowBlocked(int state)
            => IsAny(state, Own_Blocked | Them_Blocked);

        public static void SetFriendState(ref int state, bool value)
            => Set(ref state, Own_Friend_requested, value);

        public static void SetBlockState(ref int state, bool value)
            => Set(ref state, Own_Blocked, value);
    }

    #endregion
    // -------------------------------------------------------------------

}