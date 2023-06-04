/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;

namespace Arteranos.Avatar
{
    public class HitBox : MonoBehaviour, IHitBox
    {
        public IAvatarBrain Brain { get; set; } = null;
        public bool interactable 
        {
            get => VisibleCollider.activeSelf;
            set
            {
                VisibleCollider.SetActive(value);
                InvisibleCollider.SetActive(!value);
            }
        }

        private float fullHeight = -1;
        private bool lastInSight = false;
        private float stableDuration = 0;
        private bool triggered = false;
        private UI.INameplateUI np = null;

        // TODO Popup and Popout durations
        private const float k_PopupTime = 0.5f;
        private const float k_PopoutTime = 5.0f;

        private GameObject VisibleCollider= null;
        private GameObject InvisibleCollider = null;

        private void Awake()
        {
            VisibleCollider = transform.GetChild(0).gameObject;
            InvisibleCollider= transform.GetChild(1).gameObject;
        }

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
                    np = UI.NameplateUIFactory.New(Brain.gameObject);
                }

                if(!lastInSight && stableDuration > k_PopoutTime)
                {
                    triggered = true;
                    np?.gameObject.SetActive(false);
                }
            }
        }

        private void OnEnable()
        {
            lastInSight = false;
            stableDuration= 0;
            triggered= false;
        }

        private void OnDisable()
        {
            np?.gameObject.SetActive(false);
        }

        private void UpdateAvatarHeight()
        {
            fullHeight = Brain.Body.FullHeight;

            CapsuleCollider[] ccs = GetComponentsInChildren<CapsuleCollider>();
            foreach(CapsuleCollider cc in ccs) 
            {
                cc.height = fullHeight;
                cc.transform.localPosition = new Vector3(0, fullHeight / 2, 0);
            }
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
