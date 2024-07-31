/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */


namespace Arteranos.Avatar
{
    public interface IBubbleCoordinator
    {
        IAvatarBrain Brain { get; set; }

        void ChangeBubbleSize(float diameter, bool isFriend);
        void NotifyTrigger(IAvatarBrain touchy, bool isFriend, bool entered);
    }
}
