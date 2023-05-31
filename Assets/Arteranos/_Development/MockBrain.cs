/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arteranos.Avatar
{
    public class MockBrain : MonoBehaviour, IAvatarBrain
    {
        public string AvatarURL => "6394c1e69ef842b3a5112221";

        public int ChatOwnID => 9999;

        public uint NetID => 9999;

        public byte[] UserHash => new byte[0];

        public string Nickname => "TI-99 4a";

        public int AppearanceStatus { 
            get => m_NetMuteStatus;
            set 
            {
                m_NetMuteStatus= value;
                OnAppearanceStatusChanged?.Invoke(m_NetMuteStatus);
            } 
        }

        public void NotifyBubbleBreached(IAvatarBrain touchy, bool isFriend, bool entered)
        {
            // TODO People management
            bool haveFriends = true;

            // Not for the desired sphere of influence
            if(isFriend != haveFriends) return;

            if(entered)
                touchy.AppearanceStatus |= Avatar.AppearanceStatus.Bubbled;
            else
                touchy.AppearanceStatus &= ~Avatar.AppearanceStatus.Bubbled;
        }

        public bool IsOwned => false;

        public IAvatarLoader Body => GetComponent<IAvatarLoader>();

        public event Action<string> OnAvatarChanged;
        public event Action<int> OnAppearanceStatusChanged;

        [SerializeField] private int m_NetMuteStatus = 0;

        private void Start()
        {
            if(!IsOwned)
                AvatarHitBoxFactory.New(this);
        }

    }
}

#endif
