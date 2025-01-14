/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Arteranos.Core
{
    /// <summary>
    /// Acts as a blueprint archive to instantiate game objects throughout the application,
    /// independent to other gameobjects and components.
    /// </summary>

    [CreateAssetMenu(fileName = "Blueprints", menuName = "Scriptable Objects/Application/Blueprint Settings")]
    public class Blueprints : ScriptableObject
    {
        public EmojiSettings EmojiSettings;

        public Texture2D Unknown_Icon;

        public AudioMixer AudioMixer;

        public GameObject Avatar_Loading_StandIn;

        public RuntimeAnimatorController AvatarAnimController;

        public TextAsset RPMBoneTranslation;

        public TextAsset PrivacyTOSNotice;

        public GameObject NetworkedWorldObject;

        public GameObject TeleportReticle;

        public GameObject ClickTargetReticle;

        public GameObject GrabTargetReticle;

        [Serializable]
        public struct UI_
        {
            public GameObject Dialog;
            public GameObject AgreementDialog;
            public GameObject KickBan;
            public GameObject Progress;
            public GameObject TextMessage;
            public GameObject WorldEditor;
            public GameObject AddAvatar;
            public GameObject LicenseText;
            public GameObject ServerInfo;
            public GameObject ServerUserList;
            public GameObject UserHUD;
        };
        public UI_ UI;

        [Serializable]
        public struct InApp_
        {
            public GameObject AvatarHitBox;
            public GameObject CameraDrone;
            public GameObject Nameplate;
            public GameObject PrivacyBubble;
        }

        public InApp_ InApp;

        [Serializable]
        public struct UIComponents_
        {
            public GameObject ServerListItem;
            public GameObject SserverUserListItem;
            public GameObject UserListItem;
            public GameObject WorldPanelItem;
        }

        public UIComponents_ UIComponents;

        [Serializable]
        public struct WorldEdit_
        {
            public GameObject WorldObjectRoot;
        }

        public WorldEdit_ WorldEdit;
    }

    public static class BP
    {
        public static Blueprints I;
    }
}