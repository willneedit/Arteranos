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
    public class UserPanel_Outgoing : UserPanelBase
    {
        public override IEnumerable<SocialListEntryJSON> GetSocialListTab()
        {
            IEnumerable<SocialListEntryJSON> list = cs.GetSocialList(null, IsFriendOffered);
            foreach(SocialListEntryJSON entry in list) yield return entry;
        }

        private bool IsFriendOffered(SocialListEntryJSON arg)
        {
            return !SocialState.IsState(arg.State, SocialState.Them_Friend_offered) 
                && SocialState.IsState(arg.State, SocialState.Own_Friend_offered);
        }
    }
}
