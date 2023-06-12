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
            // FIXME Collection was modified
            IEnumerable<SocialListEntryJSON> list = cs.GetSocialList(null, IsFriends);
            foreach(SocialListEntryJSON entry in list) yield return entry;
        }

        private bool IsFriends(SocialListEntryJSON arg)
        {
            if(!SocialState.IsState(arg.state, SocialState.Own_Friend_offered)) return false;

            IAvatarBrain targetUser = SettingsManager.GetOnlineUser(arg.UserID);

            if(targetUser == null)
                // Last I've seen....
                return (SocialState.IsState(arg.state, SocialState.Own_Friend_bonded));
            else
                // Or, just in case, update the status
                return XRControl.Me.IsMutualFriends(targetUser);
        }
    }
}
