/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Arteranos.Avatar;

namespace Arteranos.UI
{
    public class HitBox : MonoBehaviour, IHitBox
    {
        public IAvatarBrain Brain { get; set; } = null;

        private float fullHeight = -1;
        private bool lastInSight = false;
        private float stableDuration = 0;
        private bool triggered = false;
        private INameplateUI np = null;

        // TODO Popup and Popout durations
        private const float k_PopupTime = 0.5f;
        private const float k_PopoutTime = 5.0f;

        private void Update()
        {
            if(fullHeight != Brain.Body.FullHeight)
                UpdateAvatarHeight();

            stableDuration += Time.deltaTime;

            if(!triggered)
            {
                if(lastInSight && stableDuration > k_PopupTime)
                {
                    triggered = true;
                    np = NameplateUIFactory.New(Brain.gameObject);
                }

                if(!lastInSight && stableDuration > k_PopoutTime)
                {
                    triggered = true;
                    np?.gameObject.SetActive(false);
                }
            }
        }

        private void UpdateAvatarHeight()
        {
            fullHeight = Brain.Body.FullHeight;

            CapsuleCollider cc = GetComponent<CapsuleCollider>();
            cc.height = fullHeight;
            transform.localPosition = new Vector3(0, fullHeight / 2, 0);
        }

        public void OnTargeted(bool inSight)
        {
            if(lastInSight != inSight)
            {
                lastInSight = inSight;
                stableDuration = 0;
                triggered = false;
            }
        }
    }
}
