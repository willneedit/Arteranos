using Arteranos;
using Arteranos.Core;
using Arteranos.XR;
using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine.Assertions;

namespace UnityEngine.XR.Interaction.Toolkit
{
    /// <summary>
    /// The <see cref="TeleportationProvider"/> is responsible for moving the XR Origin
    /// to the desired location on the user's request.
    /// </summary>
    [AddComponentMenu("XR/Locomotion/Crtled Tele Provider", 11)]
    public class CTeleProvider : TeleportationProvider
    {
        private ActionBasedContinuousMoveProvider MoveProvider = null;

        public TeleportType TeleportType = TeleportType.Instant;
        public float TravelDuration = 0.0f;

        private IEnumerator MoveToDestination(Vector3 src, Vector3 dest)
        {
            float progress = 0.0f;

            // Suspend the gravity for the teleport travel duration
            bool hadGravity = MoveProvider.useGravity;
            MoveProvider.useGravity = false;

            while(true)
            {
                yield return null;

                progress += Time.deltaTime;

                float t = progress / TravelDuration;
                Vector3 actual = Vector3.Lerp(src, dest, t);

                system.xrOrigin.MoveCameraToWorldLocation(actual);
                Physics.SyncTransforms();

                if(t >= 1.0f) break;
            }

            MoveProvider.useGravity = hadGravity;
            EndLocomotion();
        }

        private IEnumerator BlinkToDestination(Vector3 dest)
        {
            ScreenFader.StartFading(1.0f, 0.25f);
            yield return new WaitForSeconds(0.25f);

            system.xrOrigin.MoveCameraToWorldLocation(dest);
            Physics.SyncTransforms();

            ScreenFader.StartFading(0.0f, 0.25f);
            yield return new WaitForSeconds(0.25f);

            EndLocomotion();
        }


        protected override void Awake()
        {
            base.Awake();

            MoveProvider = GetComponent<ActionBasedContinuousMoveProvider>();
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected override void Update()
        {
            if (!validRequest || !BeginLocomotion())
                return;

            XROrigin xrOrigin = system.xrOrigin;
            if (xrOrigin != null)
            {
                switch (currentRequest.matchOrientation)
                {
                    case MatchOrientation.WorldSpaceUp:
                        xrOrigin.MatchOriginUp(Vector3.up);
                        break;
                    case MatchOrientation.TargetUp:
                        xrOrigin.MatchOriginUp(currentRequest.destinationRotation * Vector3.up);
                        break;
                    case MatchOrientation.TargetUpAndForward:
                        xrOrigin.MatchOriginUpCameraForward(currentRequest.destinationRotation * Vector3.up, currentRequest.destinationRotation * Vector3.forward);
                        break;
                    case MatchOrientation.None:
                        // Change nothing. Maintain current origin rotation.
                        break;
                    default:
                        Assert.IsTrue(false, $"Unhandled {nameof(MatchOrientation)}={currentRequest.matchOrientation}.");
                        break;
                }

                Vector3 heightAdjustment = xrOrigin.Origin.transform.up * xrOrigin.CameraInOriginSpaceHeight;

                Vector3 cameraDestination = currentRequest.destinationPosition + heightAdjustment;

                if(TeleportType == TeleportType.Instant)
                {
                    xrOrigin.MoveCameraToWorldLocation(cameraDestination);
                    Physics.SyncTransforms();
                    EndLocomotion();
                }
                else if(TeleportType == TeleportType.Blink)
                {
                    StartCoroutine(BlinkToDestination(cameraDestination));
                }
                else if(TeleportType == TeleportType.Zipline)
                {
                    StartCoroutine(MoveToDestination(
                        xrOrigin.Origin.transform.position + heightAdjustment,
                        cameraDestination));
                }
            }

            validRequest = false;
        }
    }
}
