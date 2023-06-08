/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections;
using System.Collections.Generic;

using Arteranos.Core;
using Arteranos.Avatar;
using Arteranos.Social;
using Arteranos.XR;

namespace Arteranos.UI
{
    public class FriendPanel_Friends : FriendPanelBase
    {
        public override IEnumerable<SocialListEntryJSON> GetSocialListTab()
        {
            IEnumerable<SocialListEntryJSON> list = cs.GetSocialList(null, IsFriends);
            foreach(SocialListEntryJSON entry in list) yield return entry;
        }

        private bool IsFriends(SocialListEntryJSON arg)
        {
            if((arg.state & SocialState.Friend_offered) != SocialState.Friend_offered) return false;

            IAvatarBrain targetUser = SettingsManager.GetOnlineUser(arg.UserID);

            if(targetUser == null)
                // Last I've seen....
                return (arg.state & SocialState.Friend_bonded) == SocialState.Friend_bonded;
            else
                // Or, just in case, update the status
                return XRControl.Me.IsMutualFriends(targetUser);
        }
    }
}
