/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections.Generic;

using Arteranos.Core;
using Arteranos.Social;

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
            if(!SocialState.IsState(arg.state, SocialState.Own_Friend_offered)) return false;

            return (SocialState.IsState(arg.state, 
                SocialState.Own_Friend_offered | SocialState.Them_Friend_offered));
        }
    }
}
