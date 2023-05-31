/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;

namespace Arteranos.Avatar
{
    public class BubbleCoordinator : MonoBehaviour, IBubbleCoordinator
    {
        public IAvatarBrain Brain { get; set; } = null;

        public SphereCollider Friend = null;
        public SphereCollider Stranger = null;


        void Start()
        {
            Friend = transform.GetChild(0).GetComponent<SphereCollider>();
            Stranger = transform.GetChild(1).GetComponent<SphereCollider>();

            // TODO Update for avatar reconfiguring
            transform.localPosition += transform.rotation * Vector3.up * Brain.Body.FullHeight / 2;
        }

        public void ChangeBubbleSize(float diameter, bool isFriend)
        {
            SphereCollider coll = isFriend ? Friend : Stranger;

            coll.radius = diameter / 2;
        }

        public void NotifyTrigger(IAvatarBrain touchy, bool isFriend, bool entered)
        {
            //Debug.Log($"Hit? {entered}, Who: {touchy.Nickname}, Friendzone? {isFriend}");

            Brain.NotifyBubbleBreached(touchy, isFriend, entered);
        }
    }
}
