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
        public override IEnumerable<KeyValuePair<UserID, ulong>> GetSocialListTab()
        {
            IEnumerable<KeyValuePair<UserID, ulong>> list = cs.GetSocialList(null, IsFriends);
            foreach(KeyValuePair<UserID, ulong> entry in list) yield return entry;
        }

        private bool IsFriends(KeyValuePair<UserID, ulong> arg)
            => SocialState.IsBlocked(arg.Value);
    }
}
