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
    public class BubbleCoordinator : MonoBehaviour, IBubbleCoordinator
    {
        public IAvatarBrain Brain { get; set; } = null;

        private SphereCollider Friend = null;
        private SphereCollider Stranger = null;

        private readonly Client cs = SettingsManager.Client;

        private void OnEnable() => cs.OnPrivacyBubbleChanged += OnPrivacyBubbleChanged;

        private void OnDisable() => SettingsManager.Client.OnPrivacyBubbleChanged -= OnPrivacyBubbleChanged;

        private void OnPrivacyBubbleChanged(float friend, float stranger)
        {
            ChangeBubbleSize(friend, true);
            ChangeBubbleSize(stranger, false);
        }

        void Start()
        {
            Friend = transform.GetChild(0).GetComponent<SphereCollider>();
            Stranger = transform.GetChild(1).GetComponent<SphereCollider>();

            ChangeBubbleSize(cs.SizeBubbleFriends, true);
            ChangeBubbleSize(cs.SizeBubbleStrangers, false);

            // TODO Update for avatar reconfiguring
            transform.localPosition += transform.rotation * Vector3.up * Brain.Body.FullHeight / 2;
        }

        public void ChangeBubbleSize(float diameter, bool isFriend)
        {
            SphereCollider coll = isFriend ? Friend : Stranger;

            coll.radius = diameter / 2;
        }

        public void NotifyTrigger(IAvatarBrain touchy, bool isFriend, bool entered)
            => Brain.NotifyBubbleBreached(touchy, isFriend, entered);
    }
}
