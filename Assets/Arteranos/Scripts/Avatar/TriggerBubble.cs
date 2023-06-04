/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Avatar;
using UnityEngine;

namespace Arteranos.UI
{

    public class TriggerBubble : MonoBehaviour
    {
        [SerializeField] private bool IsFriend = false;

        private BubbleCoordinator coord = null;

        private void OnEnable() => coord = GetComponentInParent<BubbleCoordinator>();

        private void OnTriggerEnter(Collider other) => NotifyTriggering(other.gameObject, true);

        private void OnTriggerExit(Collider other) => NotifyTriggering(other.gameObject, false);

        public void NotifyTriggering(GameObject go, bool hit)
        {
            IHitBox hb = go.GetComponentInParent<IHitBox>();
            
            if(hb == null) return;

            coord.NotifyTrigger(hb.Brain, IsFriend, hit);
        }
    }
}
