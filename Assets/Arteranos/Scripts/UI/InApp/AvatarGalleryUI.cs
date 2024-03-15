/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using UnityEngine;
using System.Collections;
using Ipfs;
using Arteranos.Core.Operations;
using System.Threading.Tasks;
using Arteranos.Services;

namespace Arteranos.UI
{
    public class AvatarGalleryUI : MonoBehaviour
    {
        [SerializeField] private Renderer btn_prev = null;
        [SerializeField] private Renderer btn_next = null;
        [SerializeField] private Renderer btn_save = null;
        [SerializeField] private Renderer btn_commit = null;
        [SerializeField] private Renderer btn_delete = null;

        private readonly Vector3 puppetPosition = new Vector3(0, 0.2f, 0);
        private readonly Quaternion puppetRotation = Quaternion.Euler(0, 180, 0);

        private UserDataSettingsJSON Me;
        private int index = 0;
        private GameObject currentAvatar = null;
        private bool dirty = false;

        private void OnEnable()
        {
            Me = SettingsManager.Client.Me;
            ShowAvatar();
        }

        private void OnDisable()
        {
            if(dirty)
                SettingsManager.Client.Save();
        }

        private void ShowAvatar()
        {
            IEnumerator LoadAvatarCoroutine(Cid AvatarCid)
            {
                if (currentAvatar != null)
                    Destroy(currentAvatar);
                currentAvatar = null;

                (AsyncOperationExecutor<Context> ao, Context co) =
                    AvatarDownloader.PrepareDownloadAvatar(AvatarCid, new()
                    {
                        InstallAnimController = true,
                        //DesiredHeight = 1.75f,
                        InstallEyeAnimation = true,
                    });

                yield return ao.ExecuteCoroutine(co);

                currentAvatar = AvatarDownloader.GetLoadedAvatar(co);
                currentAvatar.transform.SetParent(transform, false);
                currentAvatar.transform.SetLocalPositionAndRotation(
                    puppetPosition,
                    puppetRotation);
                currentAvatar.SetActive(true);
            }

            int count = Me.AvatarGallery.Count;

            LightOn(btn_save, !IsMeInGallery());
            LightOn(btn_commit, !IsEmpty() && GetMeAvatar() != Me.AvatarGallery[index]);
            LightOn(btn_prev, count > 1);
            LightOn(btn_next, count > 1);
            LightOn(btn_delete, !IsEmpty());

            if(Me.AvatarGallery.Count < 1) return;

            StartCoroutine(LoadAvatarCoroutine(Me.AvatarGallery[index].AvatarCidString));
        }

        private AvatarDescriptionJSON GetMeAvatar() => new()
        {
            AvatarCidString = Me.CurrentAvatar.AvatarCidString,
            AvatarHeight = Me.CurrentAvatar.AvatarHeight,
        };

        public bool IsMeInGallery() => Me.AvatarGallery.Contains(GetMeAvatar());

        public bool IsEmpty() => Me.AvatarGallery.Count == 0;

        public void Browse(bool next)
        {
            if(IsEmpty()) return;

            int len = Me.AvatarGallery.Count;

            index = (index + len + (next ? 1 : -1)) % len;

            ShowAvatar();
        }

        public void UseAvatar()
        {
            if(IsEmpty() || GetMeAvatar() == Me.AvatarGallery[index]) return;

            AvatarDescriptionJSON puppet = Me.AvatarGallery[index];
            SettingsManager.Client.AvatarCidString = puppet.AvatarCidString;

            LightOn(btn_commit, false);
            dirty = true;
        }

        public void SaveAvatar()
        {
            if(IsMeInGallery()) return;

            AvatarDescriptionJSON meAva = GetMeAvatar();

            Me.AvatarGallery.Insert(index, meAva);
            IPFSService.PinCid(meAva.AvatarCidString, true);
            ShowAvatar();

            dirty = true;
        }

        public void DeleteAvatar()
        {
            if(IsEmpty()) return;

            Me.AvatarGallery.RemoveAt(index);
            // TODO Only unpin when it's not the cuurent avatar, too
            ShowAvatar();

            dirty = true;
        }

        private void LightOn(Renderer btn, bool lit)
        {
            btn.material.SetColor("_EmissionColor", lit
                ? Color.gray
                : Color.black);

            btn.material.color = lit
                ? Color.gray
                : Color.black;
        }
    }
}
