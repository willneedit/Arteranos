/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;
using UnityEngine.EventSystems;

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
            
        private ClientSettings cs = null;
        private bool dirty = false;

        protected override void Awake()
        {
            base.Awake();

            sldn_bubble_friends.OnValueChanged += Sldn_bubble_friends_OnValueChanged;
            sldn_bubble_strangers.OnValueChanged += Sldn_bubble_strangers_OnValueChanged;

            spn_uid_visible.OnChanged += Spn_uid_visible_OnChanged;
            spn_uid_type.OnChanged += Spn_uid_type_OnChanged;
            spn_msg_reception.OnChanged += Spn_msg_reception_OnChanged;
        }

        private void Spn_msg_reception_OnChanged(int arg1, bool arg2)
        {
            cs.UserPrivacy.TextReception = (UserVisibility)spn_msg_reception.value;
            dirty = true;
        }

        private void Spn_uid_type_OnChanged(int arg1, bool arg2)
        {
            cs.UserPrivacy.UIDRepresentation = (UIDRepresentation)spn_uid_type.value;
            dirty = true;
        }

        private void Spn_uid_visible_OnChanged(int arg1, bool arg2)
        {
            cs.UserPrivacy.UIDVisibility = (UserVisibility)spn_uid_visible.value;
            dirty = true;
        }

        protected override void Start()
        {
            base.Start();

            cs = SettingsManager.Client;

            sldn_bubble_friends.value = cs.SizeBubbleFriends;
            sldn_bubble_strangers.value = cs.SizeBubbleStrangers;

            spn_uid_visible.value = (int) cs.UserPrivacy.UIDVisibility;
            spn_uid_type.value = (int) cs.UserPrivacy.UIDRepresentation;
            spn_msg_reception.value = (int) cs.UserPrivacy.TextReception;

            // Reset the state as it's the initial state, not the blank slate.
            dirty = false;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            // Might be to disabled before it's really started, so cs may be null yet.
            if(dirty) cs?.Save();
            dirty = false;
            cs?.PingUserPrivacyChanged();
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
