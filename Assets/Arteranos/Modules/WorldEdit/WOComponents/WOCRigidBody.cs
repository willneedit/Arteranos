/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Avatar;
using Arteranos.Core;
using ProtoBuf;
using System.IO;
using UnityEngine;

namespace Arteranos.WorldEdit.Components
{
    [ProtoContract]
    public class WOCRigidBody : WOCBase, IRigidBody
    {
        [ProtoMember(1)]
        public float Mass;
        [ProtoMember(2)]
        public float Drag;
        [ProtoMember(3)]
        public float AngularDrag;
        [ProtoMember(4)]
        public bool ObeysGravity;
        [ProtoMember(5)]
        public bool Grabbable;

        public void SetState()
        {
            Dirty = true;
        }

        public override object Clone()
        {
            return MemberwiseClone();
        }

        public override (string name, GameObject gameObject) GetUI()
            => ("Rigid Body", BP.I.WorldEdit.RigidBodyInspector);

        public override void ReplaceValues(WOCBase wOCBase)
        {
            WOCRigidBody s = wOCBase as WOCRigidBody;

            Mass = s.Mass;
            Drag = s.Drag;
            AngularDrag = s.AngularDrag;
            ObeysGravity = s.ObeysGravity;
            Grabbable = s.Grabbable;
        }

        // ---------------------------------------------------------------

        public bool IsMovable => Grabbable;
        public Rigidbody Rigidbody = null;

        // On client, suspend the Network Transform to stop receiving data.
        public void GotGrabbed()
        {
            if (G.Me != null)
            {
                G.Me.GotObjectGrabbed(GameObject);
                G.Me.ManageAuthorityOf(GameObject, true);
            }
            else ServerGotGrabbed();
        }

        public void GotReleased(Vector3 velocity, Vector3 angularVelocity)
        {
            // Debug.Log($"[Client] Detach velocities: {velocity}, {angularVelocity}");

            if (G.Me != null)
            {
                // TODO Pusk back after ArteranosGrabInteractable.Detach for the final results
                G.Me.GotObjectReleased(GameObject, velocity, angularVelocity);
                G.Me.ManageAuthorityOf(GameObject, false);
            }
            else ServerGotReleased(velocity, angularVelocity);
        }

        public void ServerGotGrabbed() { }

        // On server, receive the 'last seen' velocities and import to the physics engine
        public void ServerGotReleased(Vector3 detachVelocity, Vector3 detachAngularVelocity) 
        {
            if (!Rigidbody && !GameObject.TryGetComponent(out Rigidbody)) return;

            // Debug.Log($"[Server] Detach velocities: {detachVelocity}, {detachAngularVelocity}");

            Rigidbody.velocity = detachVelocity;
            Rigidbody.angularVelocity = detachAngularVelocity;
        }

        public void ServerGotObjectHeld(Vector3 position, Quaternion rotation) { }
    }
}