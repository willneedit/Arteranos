using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[AddComponentMenu("XR/Dynamic Teleportation Area", 11)]
public class DynamicTeleportationArea : BaseTeleportationInteractable
{
        protected override bool GenerateTeleportRequest(IXRInteractor interactor, RaycastHit raycastHit, ref TeleportRequest teleportRequest)
        {
            if (raycastHit.collider == null)
                return false;

            teleportRequest.destinationPosition = raycastHit.point;
            teleportRequest.destinationRotation = transform.rotation;
            return true;
        }
        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            if(teleportationProvider == null)
                teleportationProvider = FindObjectOfType<TeleportationProvider>();
            
            base.OnSelectEntered(args);
        }

        /// <inheritdoc />
        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            if(teleportationProvider == null)
                teleportationProvider = FindObjectOfType<TeleportationProvider>();
            
            base.OnSelectExited(args);
        }

        /// <inheritdoc />
        protected override void OnActivated(ActivateEventArgs args)
        {
            if(teleportationProvider == null)
                teleportationProvider = FindObjectOfType<TeleportationProvider>();
            
            base.OnActivated(args);
        }

        /// <inheritdoc />
        protected override void OnDeactivated(DeactivateEventArgs args)
        {
            if(teleportationProvider == null)
                teleportationProvider = FindObjectOfType<TeleportationProvider>();
            
            base.OnDeactivated(args);
        }

}
