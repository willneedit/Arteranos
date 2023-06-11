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
    public class FriendPanel_Incoming : FriendPanelBase
    {
        public override IEnumerable<SocialListEntryJSON> GetSocialListTab()
        {
            Dictionary<UserID, SocialListEntryJSON> list = new();

            // Get the currently logged-in users with the default state....
            foreach(IAvatarBrain user in SettingsManager.GetOnlineUsers())
            {
                if(user.UserID == XRControl.Me.UserID) continue;

                list[user.UserID] = new SocialListEntryJSON()
                {
                    UserID = user.UserID,
                    Nickname = user.Nickname,
                    state = (XRControl.Me.GetReflectiveState(user) << 16) 
                        + XRControl.Me.GetOwnState(user),
                };
            }

            foreach(KeyValuePair<UserID, SocialListEntryJSON> entry in list)
            {
                if(SocialState.IsState(entry.Value.state, SocialState.Friend_offered)) continue;

                if(!SocialState.IsState(entry.Value.state, SocialState.Friend_offered << 16)) continue;

                yield return entry.Value;
            }
        }
    }
}
