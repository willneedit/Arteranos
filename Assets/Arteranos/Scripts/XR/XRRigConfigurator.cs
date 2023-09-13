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
using UnityEngine.XR.Interaction.Toolkit.Inputs;

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
        private InputActionManager InputActionManager = null;

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
            InputActionManager = GetComponent<InputActionManager>();
        }

        private void Start()
        {
            SettingsManager.Client.OnXRControllerChanged += DownloadControlSettings;
            SettingsManager.Client.PingXRControllersChanged();
        }

        private void OnDestroy() => SettingsManager.Client.OnXRControllerChanged -= DownloadControlSettings;


        private void DownloadControlSettings(ControlSettingsJSON ccs, MovementSettingsJSON mcs)
        {
            if (ccs == null) return;

            if (LeftInteractor != null)
            {
                LeftInteractor.enabled = ccs.Controller_left;
                LeftLineVisual.enabled = ccs.Controller_left;

                LeftLineVisual.invalidColorGradient = ccs.Controller_active_left
                    ? alwaysVisibleRay
                    : onlyValidVisibleRay;

                LeftInteractor.lineType = ccs.Controller_Type_left == RayType.Straight
                    ? XRRayInteractor.LineType.StraightLine
                    : XRRayInteractor.LineType.ProjectileCurve;

                LeftInteractor.acceleration = ccs.Controller_Type_left == RayType.HighArc
                    ? 5.0f
                    : 9.8f;
            }

            if (RightInteractor != null)
            {
                RightInteractor.enabled = ccs.Controller_right;
                RightLineVisual.enabled = ccs.Controller_right;

                RightLineVisual.invalidColorGradient = ccs.Controller_active_right
                    ? alwaysVisibleRay
                    : onlyValidVisibleRay;

                RightInteractor.lineType = ccs.Controller_Type_right == RayType.Straight
                    ? XRRayInteractor.LineType.StraightLine
                    : XRRayInteractor.LineType.ProjectileCurve;

                RightInteractor.acceleration = ccs.Controller_Type_right == RayType.HighArc
                    ? 5.0f
                    : 9.8f;
            }

            // TODO - user privilege management and server restrictions
            // TODO - movement vector direction needs the y component when flying is enabled
            MoveProvider.useGravity = !mcs.Flying;

            TurnType turnType = mcs.Turn;
            StartCoroutine(ReconfigureTurnType(turnType));

            ContTurnProvider.turnSpeed = mcs.SmoothTurnSpeed;

            CTeleProvider.TravelDuration = mcs.Teleport == TeleportType.Instant
                ? 0.0f
                : mcs.ZipLineDuration;

            CTeleProvider.TeleportType = mcs.Teleport;
        }

        private IEnumerator ReconfigureTurnType(TurnType turnType)
        {
            // Issue #52: Unity's Input system occasionally borks if input handlers
            // have been disabled/enabled too quickly.
            // TODO Maybe with FreezeControl for the VR keyboard?
            yield return new WaitForSeconds(0.25f);

            InputActionManager.enabled = false;

            yield return new WaitForSeconds(0.25f);

            SnapTurnProvider.enabled = false;
            ContTurnProvider.enabled = false;

            yield return new WaitForSeconds(0.25f);

            SnapTurnProvider.enabled = turnType != TurnType.Smooth;
            ContTurnProvider.enabled = turnType == TurnType.Smooth;

            switch (turnType)
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

            yield return new WaitForSeconds(0.25f);

            InputActionManager.enabled = true;
        }
    }
}
