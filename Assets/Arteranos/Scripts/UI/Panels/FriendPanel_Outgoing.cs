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
    public class FriendPanel_Outgoing : FriendPanelBase
    {
        public override IEnumerable<SocialListEntryJSON> GetSocialListTab()
        {
            // FIXME Collection was modified
            IEnumerable<SocialListEntryJSON> list = cs.GetSocialList(null, IsFriendOffered);
            foreach(SocialListEntryJSON entry in list) yield return entry;
        }

        private bool IsFriendOffered(SocialListEntryJSON arg)
        {
            return !SocialState.IsState(arg.state, SocialState.Friend_bonded) 
                && SocialState.IsState(arg.state, SocialState.Friend_offered);
        }
    }
}
