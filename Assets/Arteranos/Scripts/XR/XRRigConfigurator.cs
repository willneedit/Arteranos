/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.XR.Interaction.Toolkit;

using Arteranos.Core;

namespace Arteranos.XR
{
    public class XRRigConfigurator : MonoBehaviour
    {
        [SerializeField] private XRRayInteractor LeftInteractor = null;
        [SerializeField] private XRInteractorLineVisual LeftLineVisual = null;
        [SerializeField] private XRRayInteractor RightInteractor = null;
        [SerializeField] private XRInteractorLineVisual RightLineVisual = null;

        private ActionBasedSnapTurnProvider SnapTurnProvider = null;
        private ActionBasedContinuousTurnProvider ContTurnProvider = null;
        private ActionBasedContinuousMoveProvider MoveProvider = null;
        private CTeleProvider CTeleProvider = null;

        private readonly Gradient onlyValidVisibleRay = new()
        {
            colorKeys = new[] { new GradientColorKey(Color.red, 0f), new GradientColorKey(Color.red, 1f) },
            alphaKeys = new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0f, 1f) },
        };

        private readonly Gradient alwaysVisibleRay = new()
        {
            colorKeys = new[] { new GradientColorKey(Color.red, 0f), new GradientColorKey(Color.red, 1f) },
            alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) },
        };

        private void Awake()
        {
            SnapTurnProvider = GetComponent<ActionBasedSnapTurnProvider>();
            ContTurnProvider = GetComponent<ActionBasedContinuousTurnProvider>();
            MoveProvider     = GetComponent<ActionBasedContinuousMoveProvider>();
            CTeleProvider    = GetComponent<CTeleProvider>();
        }

        private void Start()
        {
            SettingsManager.Client.OnXRControllerChanged += DownloadControlSettings;
            DownloadControlSettings();
        }

        private void OnDestroy() => SettingsManager.Client.OnXRControllerChanged -= DownloadControlSettings;


        private void DownloadControlSettings()
        {
            ControlSettingsJSON ccs = SettingsManager.Client?.Controls;
            MovementSettingsJSON mcs = SettingsManager.Client?.Movement;

            if(ccs == null) return;

            if(LeftInteractor != null)
            {
                LeftInteractor.enabled = ccs.controller_left;
                LeftLineVisual.enabled = ccs.controller_left;

                LeftLineVisual.invalidColorGradient = ccs.controller_active_left
                    ? alwaysVisibleRay 
                    : onlyValidVisibleRay;

                LeftInteractor.lineType = ccs.controller_Type_left == RayType.Straight
                    ? XRRayInteractor.LineType.StraightLine
                    : XRRayInteractor.LineType.ProjectileCurve;

                LeftInteractor.acceleration = ccs.controller_Type_left == RayType.HighArc
                    ? 5.0f
                    : 9.8f;
            }

            if(RightInteractor != null)
            {
                RightInteractor.enabled = ccs.controller_right;
                RightLineVisual.enabled = ccs.controller_right;

                RightLineVisual.invalidColorGradient = ccs.controller_active_right
                    ? alwaysVisibleRay
                    : onlyValidVisibleRay;

                RightInteractor.lineType = ccs.controller_Type_right == RayType.Straight
                    ? XRRayInteractor.LineType.StraightLine
                    : XRRayInteractor.LineType.ProjectileCurve;

                RightInteractor.acceleration = ccs.controller_Type_right == RayType.HighArc
                    ? 5.0f
                    : 9.8f;
            }

            // TODO - user privilege management and server restrictions
            // TODO - movement vector direction needs the y component when flying is enabled
            MoveProvider.useGravity = !mcs.Flying;

            SnapTurnProvider.enabled = mcs.Turn != TurnType.Smooth;
            ContTurnProvider.enabled = mcs.Turn == TurnType.Smooth;

            switch(mcs.Turn)
            {
                case TurnType.Snap90:
                    SnapTurnProvider.turnAmount = 90;
                    break;
                case TurnType.Snap45:
                    SnapTurnProvider.turnAmount = 45;
                    break;
                case TurnType.Snap30:
                    SnapTurnProvider.turnAmount = 30;
                    break;
                case TurnType.Snap225:
                    SnapTurnProvider.turnAmount = 22.5f;
                    break;
            }

            // TODO - continuous turn speed

            // TODO - extend the teleport provider
        }
    }
}
