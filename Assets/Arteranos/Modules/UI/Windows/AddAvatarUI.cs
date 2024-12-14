/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */


using Arteranos.Avatar;
using Arteranos.Core;
using Arteranos.Core.Operations;
using Ipfs;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Arteranos.UI
{
    public class AddAvatarUI : ActionPage
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
        [SerializeField] private Button btn_ToFileBrowser;
        [SerializeField] private Button btn_AddAvatar;
        [SerializeField] private Button btn_AddToGallery;

        private Transform PreviewSpace = null;
        private GameObject Avatar = null;
        private string LastPreviewedURL = null;
        private Cid AvatarCid = null;

        private const string tc_LoadAvatar = "Load Avatar";
        private const string tc_SetCurrent = "Set Current";

        private string btn_LabelAddAvatar
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
            throw new NotImplementedException();
        }

        protected override void Awake()
        {
            base.Awake();

            PreviewSpace = obj_Placeholder.transform.parent;
            obj_Placeholder.SetActive(false);

            btn_Close.onClick.AddListener(() => BackOut(null));
            btn_AddAvatar.onClick.AddListener(OnAddAvatarClicked);
            btn_AddToGallery.onClick.AddListener(OnAddToGalleryClicked);
            btn_ToFileBrowser.onClick.AddListener(() => ActionRegistry.Call(
                "fileBrowser",
                new FileBrowserData() { Pattern = @".*\.(glb|gltf)" },
                callback: r => { if (r != null) txt_AddAvatarModelURL.text = r.ToString(); }));
            
            txt_AddAvatarModelURL.onValueChanged.AddListener(OnAvatarURLChanged);

            btn_AddToGallery.gameObject.SetActive(false);
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
            bool unchanged = LastPreviewedURL == txt_AddAvatarModelURL.text;
            btn_LabelAddAvatar = unchanged
                ? tc_SetCurrent
                : tc_LoadAvatar;
            btn_AddToGallery.gameObject.SetActive(unchanged);
        }


        private void OnAddAvatarClicked()
        {
            IEnumerator UploadAvatarCoroutine()
            {
                yield return null;
                Cid AssetCid = null;

                {
                    string sourceURL = txt_AddAvatarModelURL.text;

                    // Naked Ready Player Me URL fixup...
                    //  - Necessary morph targets
                    //  - T-pose, not A-pose
                    if((sourceURL.StartsWith("https://models.readyplayer.me/") ||
                        sourceURL.StartsWith("http://models.readyplayer.me/")) &&
                        sourceURL.EndsWith(".glb"))
                    {
                        sourceURL += "?pose=T&morphTargets=eyeBlinkLeft,eyeBlinkRight,mouthOpen";
                    }

                    (AsyncOperationExecutor<Context> ao, Context co) =
                        AssetUploader.PrepareUploadToIPFS(sourceURL, false); // Plain GLB file

                    ao.ProgressChanged += (ratio, msg) => lbl_Notice.text = $"{msg}";

                    AggregateException ex = null;
                    yield return ao.ExecuteCoroutine(co, (_status, _) =>  ex = _status);

                    if(ex != null)
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
                        G.AvatarDownloader.PrepareDownloadAvatar(AssetCid, new AvatarDownloaderOptions()
                        {
                            InstallAnimController = true,
                        });

                    ao.ProgressChanged += (ratio, msg) => lbl_Notice.text = $"{msg}";

                    AggregateException ex = null;
                    yield return ao.ExecuteAllCoroutines(co, (_ex, _) => ex = _ex);

                    if (ex != null)
                    {
                        lbl_Notice.text = "Failed to decode the avatar model";
                        btn_AddAvatar.interactable = true;
                        yield break;
                    }

                    Avatar = G.AvatarDownloader.GetLoadedAvatar(co);

                    IObjectStats ar = G.AvatarDownloader.GetAvatarRating(co);

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
                    AvatarCid = AssetCid;
                    OnAvatarURLChanged(LastPreviewedURL);
                }
                else
                {
                    obj_Placeholder.SetActive(true);
                }
                btn_AddAvatar.interactable = true;
            }

            if(btn_LabelAddAvatar == tc_SetCurrent)
            {
                Client cs = G.Client;

                //if (cs.AvatarCidString != null)
                //    IPFSService.PinCid(cs.AvatarCidString, false);

                // Save this as the current avatar and keep it.
                cs.AvatarCidString = AvatarCid;
                cs.Save();
                _ = G.IPFSService.PinCid(AvatarCid, true);

                Destroy(gameObject);
            }
            else
            {
                btn_AddAvatar.interactable = false;
                StartCoroutine(UploadAvatarCoroutine());
            }
        }

        private void OnAddToGalleryClicked()
        {
            Client client = G.Client;

            AvatarDescriptionJSON newAva = new()
            {
                AvatarCidString = AvatarCid,
                AvatarHeight = client.AvatarHeight,
            };

            if (client.Me.AvatarGallery.Contains(newAva)) return;
            
            client.Me.AvatarGallery.Add(newAva);
            G.IPFSService.PinCid(AvatarCid, true);

            lbl_Notice.text = "Avatar stored in Gallery";

            txt_AddAvatarModelURL.text = string.Empty;
            OnAvatarURLChanged(LastPreviewedURL);

            client.Save();
        }
    }
}