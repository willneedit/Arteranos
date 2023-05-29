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

        public int NetMuteStatus { 
            get => m_NetMuteStatus;
            set 
            {
                m_NetMuteStatus= value;
                OnNetMuteStatusChanged?.Invoke(m_NetMuteStatus);
            } 
        }

        public bool IsOwned => false;

        public bool ClientMuted { get => m_ClientMuted; set => m_ClientMuted = value; }

        public IAvatarLoader Body => GetComponent<IAvatarLoader>();

        public event Action<string> OnAvatarChanged;
        public event Action<int> OnNetMuteStatusChanged;

        [SerializeField] private int m_NetMuteStatus = 0;
        [SerializeField] private bool m_ClientMuted = false;

        private void Start()
        {
            if(!IsOwned)
                AvatarHitBoxFactory.New(gameObject);
        }

    }
}

#endif
