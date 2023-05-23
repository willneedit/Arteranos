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

using ReadyPlayerMe;

namespace Arteranos.UI
{
    public class CreateAvatarUI : UIBehaviour
    {
        [SerializeField] private AvatarCreatorStateMachine avatarCreatorStateMachine;

        private ClientSettings cs = null;

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();

            cs = SettingsManager.Client;

        }

        protected override void OnEnable()
        {
            base.OnEnable();

            avatarCreatorStateMachine.AvatarSaved += OnAvatarSaved;
        }
        protected override void OnDisable()
        {
            base.OnDisable();

            avatarCreatorStateMachine.AvatarSaved -= OnAvatarSaved;
        }

        private void OnAvatarSaved(string avatarId)
        {
            avatarCreatorStateMachine.gameObject.SetActive(false);

            SysMenuKind.CloseSystemMenus();

            cs.AvatarURL = avatarId;
            cs.Me.AvatarProvider = AvatarProvider.RPM;
        }
    }
}
