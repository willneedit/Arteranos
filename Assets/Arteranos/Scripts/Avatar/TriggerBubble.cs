/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Avatar;
using Codice.Client.Common;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arteranos.UI
{

    public class TriggerBubble : MonoBehaviour
    {
        [SerializeField] private BubbleCoordinator coord = null;
        [SerializeField] private bool IsFriend = false;

        private void OnEnable()
        {
            coord = GetComponentInParent<BubbleCoordinator>();
        }


        private void OnTriggerEnter(Collider other)
        {
            NotifyTriggering(other.gameObject, true);
        }

        private void OnTriggerExit(Collider other)
        {
            NotifyTriggering(other.gameObject, false);
        }

        public void NotifyTriggering(GameObject go, bool hit)
        {
            IHitBox hb = go.GetComponent<IHitBox>();
            
            if(hb == null) return;

            coord.NotifyTrigger(hb.Brain, IsFriend, hit);
        }
    }
}
