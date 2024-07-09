/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using UnityEngine;

namespace Arteranos.Avatar
{
    public static class Factories
    {
        public static IHitBox NewHitBox(IAvatarBrain brain)
        {
            GameObject go = Object.Instantiate(
                BP.I.InApp.AvatarHitBox, brain.transform);
            IHitBox hitBox = go.GetComponent<IHitBox>();
            hitBox.Brain = brain;

            return hitBox;
        }

        public static IBubbleCoordinator NewBubbleCoordinator(IAvatarBrain brain)
        {
            GameObject go = Object.Instantiate(
                BP.I.InApp.PrivacyBubble, brain.transform);
            IBubbleCoordinator bc = go.GetComponent<IBubbleCoordinator>();
            bc.Brain = brain;
            return bc;
        }
    }
}
