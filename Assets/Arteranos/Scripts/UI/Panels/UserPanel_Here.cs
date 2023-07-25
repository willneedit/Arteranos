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
using Arteranos.Services;

namespace Arteranos.UI
{
    public class UserPanel_Here : UserPanelBase
    {
        public override IEnumerable<SocialListEntryJSON> GetSocialListTab()
        {
            Dictionary<UserID, SocialListEntryJSON> list = new();

            // Get the currently logged-in users with the default state....
            foreach(IAvatarBrain user in NetworkStatus.GetOnlineUsers())
            {
                if(user.UserID == XRControl.Me.UserID) continue;

                list[user.UserID] = new SocialListEntryJSON()
                {
                    UserID = user.UserID,
                    Nickname = user.Nickname,
                    state = SocialState.None,
                };
            }

            // Fill in the subset of the data in the social database.
            foreach(SocialListEntryJSON entry in cs.GetFilteredSocialList())
            {
                if(!list.ContainsKey(entry.UserID)) continue;

                list[entry.UserID].state = entry.state;
            }

            foreach(KeyValuePair<UserID, SocialListEntryJSON> entry in list) 
                yield return entry.Value;
        }

    }
}