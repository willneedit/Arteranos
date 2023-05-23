using System;
using System.Collections.Generic;
using System.Linq;
using ReadyPlayerMe.AvatarCreator;
using ReadyPlayerMe.AvatarLoader;
using UnityEngine;
using UnityEngine.UI;

namespace ReadyPlayerMe
{
    public class AssetTypeUICreator : MonoBehaviour
    {
        private const string PANEL_SUFFIX = "Panel";
        private const string BUTTON_SUFFIX = "Button";
        
        [Serializable]
        private class AssetTypeIcon
        {
            public AssetType assetType;
            public Sprite icon;
        }

        [Serializable]
        private class AssetTypeUI
        {
            public GameObject buttonPrefab;
            public Transform buttonParent;
            public GameObject panelPrefab;
            public Transform panelParent;
        }

        [SerializeField] private AssetTypeUI assetTypeUI;
        [SerializeField] private AssetTypeButton faceAssetTypeButton;
        [SerializeField] private GameObject faceAssetTypePanel;
        [SerializeField] private GameObject faceAssetPanelPrefab;
        [SerializeField] private GameObject leftSidePanelPrefab;
        [SerializeField] private List<AssetTypeIcon> assetTypeIcons;

        private Dictionary<AssetType, AssetTypeButton> assetTypeButtonsMap;
        private AssetTypeButton selectedAssetTypeButton;
        private CreatedAvatarAnimator AvatarAnimator;

        private void Awake()
        {
            AvatarAnimator = FindObjectOfType<CreatedAvatarAnimator>();
        }


        public void CreateUI(IEnumerable<AssetType> assetTypes)
        {
            AvatarAnimator.AnimateTo(0);

            assetTypeButtonsMap = new Dictionary<AssetType, AssetTypeButton>();
            PanelSwitcher.FaceTypePanel = faceAssetTypePanel;
            CreateAssetTypePanel(AssetType.SkinColor, leftSidePanelPrefab, assetTypeUI.panelParent);
            foreach (var assetType in assetTypes)
            {
                if (assetType.IsColorAsset())
                {
                    CreateAssetTypePanel(assetType, leftSidePanelPrefab, assetTypeUI.panelParent);
                }
                else if (assetType.IsFaceAsset())
                {
                    CreateAssetTypePanel(assetType, faceAssetPanelPrefab, assetTypeUI.panelParent);
                    CreateAssetTypeButton(assetType, faceAssetTypePanel.GetComponent<ScrollRect>().content.transform, () =>
                        PanelSwitcher.Switch(assetType));
                }
                else
                {
                    CreateAssetTypePanel(assetType, assetTypeUI.panelPrefab, assetTypeUI.panelParent);
                    CreateAssetTypeButton(assetType, assetTypeUI.buttonParent, () =>
                        PanelSwitcher.Switch(assetType));
                }
            }

            DefaultSelection();
            faceAssetTypeButton.AddListener(() =>
            {
                if (selectedAssetTypeButton != null)
                {
                    selectedAssetTypeButton.SetSelect(false);
                }

                DefaultSelection();
            });
        }

        public void ResetUI()
        {
            PanelSwitcher.Clear();
            AvatarAnimator.AnimateTo(0);

            foreach(var assetTypeButton in assetTypeButtonsMap)
            {
                Destroy(assetTypeButton.Value.gameObject);
            }

            faceAssetTypeButton.RemoveListener();
            assetTypeButtonsMap.Clear();
        }

        private void CreateAssetTypePanel(AssetType assetType, GameObject panelPrefab, Transform parent)
        {
            var assetTypePanel = Instantiate(panelPrefab, parent);
            assetTypePanel.name = assetType + PANEL_SUFFIX;
            assetTypePanel.SetActive(false);

            PanelSwitcher.AddPanel(assetType, assetTypePanel);
        }

        private void CreateAssetTypeButton(AssetType assetType, Transform parent, Action onClick)
        {
            var assetTypeButtonGameObject = Instantiate(assetTypeUI.buttonPrefab, parent);
            var assetTypeButton = assetTypeButtonGameObject.GetComponent<AssetTypeButton>();
            assetTypeButton.name = assetType + BUTTON_SUFFIX;
            var assetTypeIcon = assetTypeIcons.FirstOrDefault(x => x.assetType == assetType);
            if (assetTypeIcon != null)
            {
                assetTypeButton.SetIcon(assetTypeIcon.icon);
            }

            assetTypeButton.AddListener(() =>
            {
                SwitchZoomByAssetType(assetType);
                assetTypeButton.SetSelect(true);
                selectedAssetTypeButton.SetSelect(false);
                faceAssetTypeButton.SetSelect(assetType.IsFaceAsset());
                selectedAssetTypeButton = assetTypeButton;
                onClick?.Invoke();
            });
            assetTypeButtonsMap.Add(assetType, assetTypeButton);
        }

        private void DefaultSelection()
        {
            faceAssetTypeButton.SetSelect(true);
            assetTypeButtonsMap[AssetType.FaceShape].SetSelect(true);
            PanelSwitcher.Switch(AssetType.FaceShape);
            selectedAssetTypeButton = assetTypeButtonsMap[AssetType.FaceShape];
        }

        private void SwitchZoomByAssetType(AssetType assetType)
        {
                if(assetType == AssetType.Outfit)
                    AvatarAnimator.AnimateTo(0); 
                else
                    AvatarAnimator.AnimateTo(1);
        }
    }
}
