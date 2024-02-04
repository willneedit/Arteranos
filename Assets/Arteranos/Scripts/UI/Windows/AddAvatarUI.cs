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
        private string LastPreviewedURL = null;
        private Cid AvatarCid = null;

        public string btn_LabelAddAvatar
        {
            get => btn_AddAvatar.transform.GetChild(0).GetComponent<TMP_Text>().text;
            set => btn_AddAvatar.transform.GetChild(0).GetComponent<TMP_Text>().text = value;
        }

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
            txt_AddAvatarModelURL.onValueChanged.AddListener(OnAvatarURLChanged);
        }

        protected override void Start()
        {
            base.Start();

            IEnumerator RotatePreviewCoroutine()
            {
                float angle = 0.0f;

                while(true)
                {
                    angle += Time.deltaTime * 45.0f;
                    if (angle > 350.0f) angle -= 360.0f;
                    if(Avatar)
                        Avatar.transform.localRotation = Quaternion.Euler(0.0f, angle, 0.0f);

                    yield return new WaitForEndOfFrame();
                }
            }

            IEnumerator AnimateAvatarCoroutine()
            {
                while(true)
                {
                    // I know. Ugly hack.... 
                    if(Avatar)
                    {
                        Animator animator = Avatar.GetComponent<Animator>();

                        yield return new WaitForSeconds(2);

                        if (!animator) continue;
                        animator.SetInteger("IntWalkFrontBack", 1);
                        yield return new WaitForSeconds(5);

                        if (!animator) continue;
                        animator.SetInteger("IntWalkFrontBack", 0);
                        yield return new WaitForSeconds(2);

                        if (!animator) continue;
                        animator.SetInteger("IntWalkFrontBack", -1);
                        yield return new WaitForSeconds(5);

                        if (!animator) continue;
                        animator.SetInteger("IntWalkFrontBack", 0);
                        yield return new WaitForSeconds(2);

                        if (!animator) continue;
                        animator.SetInteger("IntWalkLeftRight", 1);
                        yield return new WaitForSeconds(5);

                        if (!animator) continue;
                        animator.SetInteger("IntWalkLeftRight", 0);
                        yield return new WaitForSeconds(2);

                        if (!animator) continue;
                        animator.SetInteger("IntWalkLeftRight", -1);
                        yield return new WaitForSeconds(5);

                        if (!animator) continue;
                        animator.SetInteger("IntWalkLeftRight", 0);
                        yield return new WaitForSeconds(1);

                    }
                    yield return new WaitForSeconds(1);
                }
            }

            StartCoroutine(RotatePreviewCoroutine());
            StartCoroutine(AnimateAvatarCoroutine());
        }

        private void OnAvatarURLChanged(string arg0)
        {
            btn_LabelAddAvatar = LastPreviewedURL == txt_AddAvatarModelURL.text 
                ? "Confirm"
                : "Load Avatar";
        }


        private void OnAddAvatarClicked()
        {
            IEnumerator UploadAvatarCoroutine()
            {
                yield return null;
                Cid AssetCid = null;

                {
                    string sourceURL = txt_AddAvatarModelURL.text;

                    // Naked Ready Player Me URL fixup: Request the necessary morph targets
                    if((sourceURL.StartsWith("https://models.readyplayer.me/") ||
                        sourceURL.StartsWith("http://models.readyplayer.me/")) &&
                        sourceURL.EndsWith(".glb"))
                    {
                        sourceURL += "?morphTargets=eyeBlinkLeft,eyeBlinkRight,mouthOpen";
                    }

                    (AsyncOperationExecutor<Context> ao, Context co) =
                        AssetUploader.PrepareUploadToIPFS(sourceURL);

                    ao.ProgressChanged += (ratio, msg) => lbl_Notice.text = $"{msg}";

                    Task t = ao.ExecuteAsync(co);

                    while (!t.IsCompleted) yield return new WaitForEndOfFrame();

                    if(t.IsFaulted)
                    {
                        lbl_Notice.text = "Failed to load from this URL";
                        btn_AddAvatar.interactable = true;
                        yield break;
                    }

                    Cid cid = AssetUploader.GetUploadedCid(co);
                    AssetCid = cid;
                }

                {
                    if(Avatar) Destroy(Avatar);

                    (AsyncOperationExecutor<Context> ao, Context co) =
                        AvatarDownloader.PrepareDownloadAvatar(AssetCid, new()
                        {
                            InstallAnimController = 1,
                        });

                    ao.ProgressChanged += (ratio, msg) => lbl_Notice.text = $"{msg}";

                    Task t = ao.ExecuteAsync(co);

                    while (!t.IsCompleted) yield return new WaitForEndOfFrame();

                    if (t.IsFaulted)
                    {
                        lbl_Notice.text = "Failed to decode the avatar model";
                        btn_AddAvatar.interactable = true;
                        yield break;
                    }

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
                    AvatarTransform.localPosition = new Vector3(0, -50, -25);
                    AvatarTransform.localScale = new Vector3(50, 50, 50);
                    Avatar.SetActive(true);

                    lbl_Notice.text = "Avatar successfully loaded";
                    LastPreviewedURL = txt_AddAvatarModelURL.text;
                    btn_LabelAddAvatar = "Confirm";
                    AvatarCid = AssetCid;
                }
                else
                {
                    obj_Placeholder.SetActive(true);
                }
                btn_AddAvatar.interactable = true;
            }

            if(btn_LabelAddAvatar == "Confirm")
            {
                Client cs = SettingsManager.Client;

                cs.AvatarCidString = AvatarCid;
                cs.Save();
                Destroy(gameObject);
            }
            else
            {
                btn_AddAvatar.interactable = false;
                StartCoroutine(UploadAvatarCoroutine());
            }
        }
    }
}