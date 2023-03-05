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
        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected override void Update()
        {
            if (!validRequest || !BeginLocomotion())
                return;

            var xrOrigin = system.xrOrigin;
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

                var heightAdjustment = xrOrigin.Origin.transform.up * xrOrigin.CameraInOriginSpaceHeight;

                var cameraDestination = currentRequest.destinationPosition + heightAdjustment;

                xrOrigin.MoveCameraToWorldLocation(cameraDestination);
                Physics.SyncTransforms();
            }

            EndLocomotion();
            validRequest = false;
        }
    }
}
