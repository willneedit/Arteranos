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
    public class HitBox : MonoBehaviour, IHitBox
    {
        public IAvatarBrain Brain { get; set; } = null;
        public bool Interactable 
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

        private float m_PopupTime = 0.5f;
        private float m_PopoutTime = 5.0f;

        private GameObject VisibleCollider= null;
        private GameObject InvisibleCollider = null;

        private void Awake()
        {
            VisibleCollider = transform.GetChild(0).gameObject;
            InvisibleCollider= transform.GetChild(1).gameObject;
        }
        
        private void Update()
        {
            if(G.Client != null)
            {
                m_PopupTime = G.Client.Controls.NameplateIn;
                m_PopoutTime = G.Client.Controls.NameplateOut;
            }

            if(Brain.Body?.AvatarMeasures != null && fullHeight != Brain.Body.AvatarMeasures.FullHeight)
                UpdateAvatarHeight();

            stableDuration += Time.deltaTime;

            if(!triggered)
            {
                if(lastInSight && stableDuration > m_PopupTime)
                {
                    triggered = true;
                    np = UI.Factory.NewNameplate(Brain.gameObject);
                }

                if(!lastInSight && stableDuration > m_PopoutTime)
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

        private void OnDisable() => np?.gameObject.SetActive(false);

        private void UpdateAvatarHeight()
        {
            fullHeight = Brain.Body.AvatarMeasures.FullHeight;

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
