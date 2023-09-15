/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Arteranos.Core;

namespace Arteranos.UI
{
    public class PrefPanel_Privacy : UIBehaviour
    {
        [SerializeField] private NumberedSlider sldn_bubble_friends = null;
        [SerializeField] private NumberedSlider sldn_bubble_strangers = null;

        [SerializeField] private Spinner spn_uid_visible = null;
        [SerializeField] private Spinner spn_uid_type = null;
        [SerializeField] private Spinner spn_msg_reception = null;
        [SerializeField] private Spinner spn_friendreq_filter = null;
            
        private ClientSettings cs = null;
        private bool dirty = false;

        protected override void Awake()
        {
            base.Awake();

            sldn_bubble_friends.OnValueChanged += Sldn_bubble_friends_OnValueChanged;
            sldn_bubble_strangers.OnValueChanged += Sldn_bubble_strangers_OnValueChanged;
        }

        protected override void Start()
        {
            base.Start();

            cs = SettingsManager.Client;

            sldn_bubble_friends.value = cs.SizeBubbleFriends;
            sldn_bubble_strangers.value = cs.SizeBubbleStrangers;

            // Reset the state as it's the initial state, not the blank slate.
            dirty = false;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            // Might be to disabled before it's really started, so cs may be null yet.
            if(dirty) cs?.Save();
            dirty = false;
        }

        private void Sldn_bubble_strangers_OnValueChanged(float obj)
        {
            cs.SizeBubbleStrangers = obj;
            dirty = true;
        }

        private void Sldn_bubble_friends_OnValueChanged(float obj)
        {
            cs.SizeBubbleFriends = obj;
            dirty = true;
        }
    }
}
