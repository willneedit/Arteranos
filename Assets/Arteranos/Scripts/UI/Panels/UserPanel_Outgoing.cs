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
        public override IEnumerable<KeyValuePair<UserID, ulong>> GetSocialListTab()
        {
            IEnumerable<KeyValuePair<UserID, ulong>> list = cs.GetSocialList(null, IsFriendOffered);
            foreach(KeyValuePair<UserID, ulong> entry in list) yield return entry;
        }

        private bool IsFriendOffered(KeyValuePair<UserID, ulong> arg) 
            => !SocialState.IsFriendOffered(arg.Value)
                && SocialState.IsFriendRequested(arg.Value);
    }
}
