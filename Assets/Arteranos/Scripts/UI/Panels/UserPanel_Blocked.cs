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
    public class UserPanel_Blocked : UserPanelBase
    {
        public override IEnumerable<SocialListEntryJSON> GetSocialListTab()
        {
            IEnumerable<SocialListEntryJSON> list = cs.GetSocialList(null, IsFriends);
            foreach(SocialListEntryJSON entry in list) yield return entry;
        }

        private bool IsFriends(SocialListEntryJSON arg)
            => (arg.State & SocialState.Own_Blocked) == SocialState.Own_Blocked;
    }
}
