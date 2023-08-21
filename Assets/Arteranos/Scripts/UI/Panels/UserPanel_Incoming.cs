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
    public class UserPanel_Incoming : UserPanelBase
    {
        public override IEnumerable<SocialListEntryJSON> GetSocialListTab()
        {
            IEnumerable<SocialListEntryJSON> list = cs.GetSocialList(null, IsFriendReceived);
            foreach(SocialListEntryJSON entry in list) yield return entry;
        }

        private bool IsFriendReceived(SocialListEntryJSON arg)
        {
            return SocialState.IsFriendOffered(arg.State)
                && !SocialState.IsFriendRequested(arg.State);
        }
    }
}
