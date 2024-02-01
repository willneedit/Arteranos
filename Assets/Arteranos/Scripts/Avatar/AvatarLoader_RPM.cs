/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;

using ReadyPlayerMe.AvatarLoader;

using System.Collections.Generic;
using Arteranos.Core;

namespace Arteranos.Avatar
{
    public class AvatarLoader_RPM : MonoBehaviour, IAvatarBody
    {
        private GameObject m_AvatarStandin = null;
        private bool loading = false;

        private GameObject m_AvatarGameObject = null;
        private AvatarObjectLoader m_AvatarLoader = null;
        public IAvatarMeasures AvatarMeasures { get; set; } = null;


        // Non-null: manually load a puppet avatar on init
        public string GalleryModeURL { get => m_GalleryModeURL; set => m_GalleryModeURL = value; }

        [SerializeField] private string m_GalleryModeURL = null;

        public bool Invisible
        {
            get => m_invisible;
            set
            {
                m_invisible = value;
                if(m_AvatarGameObject!= null)
                {
                    Renderer[] renderers = m_AvatarGameObject.GetComponentsInChildren<Renderer>();
                    foreach(Renderer renderer in renderers)
                        renderer.enabled = !Invisible;
                }
            }
        }

        private AvatarBrain avatarBrain = null;

        private void Awake() => m_AvatarStandin = Resources.Load<GameObject>("Avatar/Avatar_StandIn");
        private bool m_invisible = false;
        private float? DesiredHeight = null;


        public void OnEnable()
        {
            m_AvatarLoader = new AvatarObjectLoader();
            // m_AvatarLoader.SaveInProjectFolder = true;
            m_AvatarLoader.OnCompleted += AvatarLoadComplete;
            m_AvatarLoader.OnFailed += AvatarLoadFailed;

            m_AvatarGameObject = Instantiate(m_AvatarStandin);
            m_AvatarGameObject.transform.SetParent(transform, false);

#if false
            if (!string.IsNullOrEmpty(GalleryModeURL)) RequestAvatarURLChange(GalleryModeURL);
            else avatarBrain = GetComponent<AvatarBrain>();
#endif
        }

        private string last = null;
        private string present = null;
#if false
        public void RequestAvatarURLChange(string current)
        {
            if(loading || current == null || last == current) return;
            present= current;

            loading = true;
            Debug.Log("Starting avatar loading: " + current);

            m_AvatarLoader.LoadAvatar(current.ToString());
        }

        public void RequestAvatarHeightChange(float targetHeight)
        {
            if(loading) return;

            if (DesiredHeight != null && targetHeight == DesiredHeight.Value) return;

            DesiredHeight = targetHeight / 100.0f;
            Debug.Log($"Resizing avatar to {DesiredHeight}");

            last = string.Empty;

            // FIXME Maybe it needs the regular avatar loading, too?
            SetupMouthBlendShapes(null);
            Destroy(m_AvatarGameObject); m_AvatarGameObject = null;
            RequestAvatarURLChange(present);
        }
#endif

        public void ReloadAvatar(string url, float height, int gender)
        {
            throw new KeyNotFoundException();
        }

        // --------------------------------------------------------------------


        void AvatarLoadComplete(object _, CompletionEventArgs args)
        {
            if(m_AvatarGameObject != null)
                Destroy(m_AvatarGameObject);

            GetComponent<AvatarPoseDriver>().UpdateAvatarMeasures(AvatarMeasures);

            if (avatarBrain != null)
                args.Avatar.name += $"_{avatarBrain.NetID}";
            else
                args.Avatar.name += "_puppet";

            args.Avatar.transform.SetParent(transform, false);

            // Refresh the visibility state for the new avatar
            Invisible = m_invisible;

            Debug.Log("Successfully loaded avatar");
            last = present;
            loading = false;
        }

        void AvatarLoadFailed(object sender, FailureEventArgs args)
        {
            Debug.Log($"Avatar loading failed with error message: {args.Message}");
            loading = false;
        }

    }
}
