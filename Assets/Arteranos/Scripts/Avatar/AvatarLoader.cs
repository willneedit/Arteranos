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

namespace Arteranos.Avatar
{
    public class AvatarLoader : MonoBehaviour, IAvatarBody
    {
        private GameObject AvatarStandin = null;
        private bool loading = false;

        private GameObject AvatarGameObject = null;
        public IAvatarMeasures AvatarMeasures { get; set; } = null;


        public bool Invisible
        {
            get => m_invisible;
            set
            {
                m_invisible = value;
                if(AvatarGameObject!= null)
                {
                    Renderer[] renderers = AvatarGameObject.GetComponentsInChildren<Renderer>();
                    foreach(Renderer renderer in renderers)
                        renderer.enabled = !Invisible;
                }
            }
        }

        private bool m_invisible = false;

        private AvatarBrain AvatarBrain = null;
        public bool isOwned => AvatarBrain?.isOwned ?? false;

        private void Awake()
        {
            AvatarStandin = SettingsManager.BP.Avatar_Loading_StandIn;
            AvatarBrain = GetComponent<AvatarBrain>();
        }

        private void Start()
        {
            // Put up the Stand-in for the time where the avatar is loaded
            AvatarGameObject = Instantiate(AvatarStandin);
            AvatarGameObject.transform.SetParent(transform, false);
        }

        public void ReloadAvatar(string avatarCid, float height)
        {
            DateTime settleTime = DateTime.Now + TimeSpan.FromSeconds(5);

            Cid _avatarCid = avatarCid;
            float _cmheight = height;

            IEnumerator AvatarDownloaderCoroutine()
            {
                while(settleTime > DateTime.Now)
                    yield return new WaitForSeconds((settleTime - DateTime.Now).Seconds);

                (AsyncOperationExecutor<Context> ao, Context co) =
                    AvatarDownloader.PrepareDownloadAvatar(_avatarCid, new()
                    {
                        DesiredHeight = _cmheight / 100.0f,
                        InstallAnimController = true,
                        InstallEyeAnimation = true,
                        InstallMouthAnimation = true,
                        InstallFootIK = isOwned,
                        InstallFootIKCollider = isOwned,
                        InstallHandIK = isOwned,
                        InstallHandIKController = isOwned,
                    });

                Task t = ao.ExecuteAsync(co);

                while (!t.IsCompleted) yield return new WaitForEndOfFrame();

                if (AvatarGameObject)
                    Destroy(AvatarGameObject);

                if (t.IsFaulted)
                    AvatarGameObject = Instantiate(AvatarStandin);
                else
                {
                    AvatarMeasures = AvatarDownloader.GetAvatarMeasures(co);
                    AvatarGameObject = AvatarMeasures.Avatar;
                }

                if (AvatarBrain)
                    AvatarGameObject.name += $"_{AvatarBrain.NetID}";
                else
                    AvatarGameObject.name += "_puppet";

                AvatarGameObject.transform.SetParent(transform, false);

                GetComponent<AvatarPoseDriver>().UpdateAvatarMeasures(AvatarMeasures);

                AvatarGameObject.SetActive(true);
                loading = false;
            }

            if (loading) return;

            loading = true;

            StartCoroutine(AvatarDownloaderCoroutine());
        }

    }
}
