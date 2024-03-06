/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;

using Arteranos.Core;
using System.Collections;
using Ipfs;
using Arteranos.Core.Operations;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace Arteranos.Avatar
{
    public class AvatarLoader : MonoBehaviour, IAvatarBody
    {
        internal struct InternalAvatarMeasures : IAvatarMeasures
        {
            public readonly GameObject Avatar => Instantiate(BP.I.Avatar_Loading_StandIn);
            public readonly Transform CenterEye => null;
            public readonly float EyeHeight => 1.75f;
            public readonly float FullHeight => 1.86f;
            public readonly float UnscaledHeight => 1.86f;
            public readonly Transform Head => null;
            public readonly Transform LeftHand => null;
            public readonly Transform RightHand => null;
            public readonly List<string> JointNames => new();
            public readonly List<FootIKData> Feet => new();
            public readonly List<Transform> Eyes => new();
            public readonly List<MeshBlendShapeIndex> MouthOpen => new();
            public readonly List<MeshBlendShapeIndex> EyeBlinkLeft => new();
            public readonly List<MeshBlendShapeIndex> EyeBlinkRight => new();
        }

        private bool loading = false;

        private GameObject AvatarGameObject = null;
        public IAvatarMeasures AvatarMeasures { get; private set; } = null;


        public bool Invisible
        {
            get => m_invisible;
            set
            {
                m_invisible = value;
                if (!AvatarGameObject) return;

                Renderer[] renderers = AvatarGameObject.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                    renderer.enabled = !Invisible;
            }
        }

        private bool m_invisible = false;

        private AvatarBrain AvatarBrain = null;
        public bool isLocalPlayer => AvatarBrain ? AvatarBrain.isLocalPlayer : false;

        private void Awake()
        {
            AvatarMeasures = new InternalAvatarMeasures();
            AvatarBrain = GetComponent<AvatarBrain>();
 
            // Put up the Stand-in for the time where the avatar is loaded
            AvatarGameObject = AvatarMeasures.Avatar;
            AvatarGameObject.transform.SetParent(transform, false);
        }

        public void ReloadAvatar(string avatarCid, float height)
        {
            DateTime settleTime = DateTime.Now + TimeSpan.FromSeconds(5);

            IEnumerator AvatarDownloaderCoroutine()
            {
                while (settleTime > DateTime.Now)
                    yield return new WaitForSeconds((settleTime - DateTime.Now).Seconds);

                (AsyncOperationExecutor<Context> ao, Context co) =
                    AvatarDownloader.PrepareDownloadAvatar((Cid)avatarCid, new()
                    {
                        DesiredHeight = (float)height / 100.0f,
                        InstallAnimController = true,
                        InstallEyeAnimation = true,
                        InstallMouthAnimation = true,
                        InstallFootIK = isLocalPlayer,
                        InstallFootIKCollider = isLocalPlayer,
                        ReadFootJoints = true,
                        InstallHandIK = isLocalPlayer,
                        InstallHandIKController = isLocalPlayer,
                        ReadHandJoints = true,
                    });

                Task t = ao.ExecuteAsync(co);

                while (!t.IsCompleted) yield return new WaitForEndOfFrame();

                if (AvatarGameObject)
                    Destroy(AvatarGameObject);

                AvatarMeasures = t.IsFaulted 
                    ? new InternalAvatarMeasures() 
                    : AvatarDownloader.GetAvatarMeasures(co);

                AvatarGameObject = AvatarMeasures.Avatar;

                AvatarGameObject.name += AvatarBrain ? $"_{AvatarBrain.NetID}" : "_puppet";

                AvatarGameObject.transform.SetParent(transform, false);

                GetComponent<AvatarPoseDriver>().UpdateAvatarMeasures(AvatarMeasures);

                AvatarGameObject.SetActive(true);
                loading = false;
            }

            if (!loading)
            {
                loading = true;

                StartCoroutine(AvatarDownloaderCoroutine());
            }
        }
    }
}
