/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Avatar;

namespace Arteranos.UI
{
    public interface IKickBanUI
    {
        IAvatarBrain Target { get; set; }
    }
}
