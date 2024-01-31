/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */


using Arteranos.Core;
using Arteranos.Core.Operations;
using Ipfs;
using System;
using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arteranos.UI
{
    public class AddAvatarUI : UIBehaviour
    {
        [SerializeField] private Button btn_Close;
        [SerializeField] private TMP_Text lbl_DrawCalls;
        [SerializeField] private TMP_Text lbl_Triangles;
        [SerializeField] private TMP_Text lbl_Vertices;
        [SerializeField] private TMP_Text lbl_Materials;
        [SerializeField] private TMP_Text lbl_Rating;

        [SerializeField] private GameObject obj_Placeholder;

        [SerializeField] private TMP_Text lbl_Notice;

        [SerializeField] private TMP_InputField txt_AddAvatarModelURL;
        [SerializeField] private Button btn_AddAvatar;

        private Transform PreviewSpace = null;
        private GameObject Avatar = null;
        private Cid AvatarCid = null;

#if UNITY_INCLUDE_TESTS
        public string Test_AvatarURL 
        {
            get => txt_AddAvatarModelURL.text;
            set => txt_AddAvatarModelURL.text = value; 
        }

        public void Test_OnAddAvatarClicked() => OnAddAvatarClicked();
#endif

        public static AddAvatarUI New()
        {
            GameObject blueprint = Resources.Load<GameObject>("UI/UI_AddAvatar");
            blueprint.SetActive(false);
            AddAvatarUI aaui = Instantiate(blueprint).GetComponent<AddAvatarUI>();
            aaui.gameObject.SetActive(true);
            return aaui;

        }
        protected override void Awake()
        {
            base.Awake();

            PreviewSpace = obj_Placeholder.transform.parent;
            obj_Placeholder.SetActive(false);

            btn_Close.onClick.AddListener(() => Destroy(gameObject));
            btn_AddAvatar.onClick.AddListener(OnAddAvatarClicked);
        }

        private void OnAddAvatarClicked()
        {
            IEnumerator UploadAvatarCoroutine()
            {
                yield return null;
                Cid AssetCid = null;

                {
                    (AsyncOperationExecutor<Context> ao, Context co) =
                        AssetUploader.PrepareUploadToIPFS(txt_AddAvatarModelURL.text);

                    ao.ProgressChanged += (ratio, msg) => lbl_Notice.text = $"{msg}";

                    Task t = ao.ExecuteAsync(co);

                    while (!t.IsCompleted) yield return new WaitForEndOfFrame();

                    Cid cid = AssetUploader.GetUploadedCid(co);
                    AssetCid = cid;
                }

                {
                    if(Avatar) Destroy(Avatar);

                    (AsyncOperationExecutor<Context> ao, Context co) =
                        AvatarDownloader.PrepareDownloadAvatar(AssetCid);

                    ao.ProgressChanged += (ratio, msg) => lbl_Notice.text = $"{msg}";

                    Task t = ao.ExecuteAsync(co);

                    while (!t.IsCompleted) yield return new WaitForEndOfFrame();

                    Avatar = AvatarDownloader.GetLoadedAvatar(co);

                    IObjectStats ar = AvatarDownloader.GetAvatarRating(co);

                    lbl_DrawCalls.text = ar.Count.ToString();
                    lbl_Triangles.text = ar.Triangles.ToString();
                    lbl_Vertices.text = ar.Vertices.ToString();
                    lbl_Materials.text = ar.Materials.ToString();
                    lbl_Rating.text = ar.Rating switch
                    {
                        1 => "Excellent",
                        >= 0.80f => "Very good",
                        >= 0.60f => "Good",
                        >= 0.40f => "Mediocre",
                        >= 0.20f => "Poor",
                        _ => "Very poor"
                    };
                }

                if(Avatar)
                {
                    obj_Placeholder.SetActive(false);
                    Transform AvatarTransform = Avatar.transform;
                    AvatarTransform.parent = PreviewSpace;
                    // LocalPosition is (0,0,0), (0,0,0)
                    // AvatarTransform.localScale = obj_Placeholder.transform.localScale;
                    AvatarTransform.localPosition = new Vector3(0, -50, 0);
                    AvatarTransform.localScale = new Vector3(50, 50, 50);
                    Avatar.SetActive(true);
                }
                else
                {
                    obj_Placeholder.SetActive(true);
                }
                btn_AddAvatar.interactable = true;
            }

            btn_AddAvatar.interactable = false;
            StartCoroutine(UploadAvatarCoroutine());
        }
    }
}