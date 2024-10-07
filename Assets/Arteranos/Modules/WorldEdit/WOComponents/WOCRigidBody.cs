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

        private Transform transform = null;
        private WorldObjectComponent woc = null;

        private Rigidbody body = null;
        private bool wasKinematic = false;
        
        private GameObject goOrShell => woc?.HasNetworkShell
                    ? woc.HasNetworkShell.gameObject
                    : GameObject;

        public override GameObject GameObject
        {
            get => base.GameObject;
            set
            {
                base.GameObject = value;
                transform = GameObject.transform;
                GameObject.TryGetComponent(out woc);
            }
        }

        public bool IsMovable => Grabbable;

        private Rigidbody Body
        {
            get
            {
                if (!body) GameObject.TryGetComponent(out body);
                return body;
            }
        }

        private bool clientGrabbed = false;

        public override void Update()
        {
            base.Update();

            // As soon as the local playwr grabbed the object, start sending
            // the coordinates to the server for propagation
            if (clientGrabbed)
            {
                // TODO NetworkTransform interferes. Maybe temporarily disable whild grabbing?
                Debug.Log($"Sending grabbed object position: {GameObject.transform.position}");
                if (G.Me != null) G.Me.GotObjectHeld(goOrShell, GameObject.transform.position, GameObject.transform.rotation);
                else ServerGotObjectHeld(GameObject.transform.position, GameObject.transform.rotation);
            }
        }

        public void GotGrabbed()
        {
            clientGrabbed = true;

            if (G.Me != null) G.Me.GotObjectGrabbed(goOrShell);
            else ServerGotGrabbed();
        }

        public void GotReleased()
        {
            clientGrabbed = false;

            if (G.Me != null) G.Me.GotObjectReleased(goOrShell);
            else ServerGotReleased();
        }

        public void ServerGotGrabbed()
        {
        }

        public void ServerGotReleased()
        {
        }

        public void ServerGotObjectHeld(Vector3 position, Quaternion rotation)
        {
            // Transfer the data to the server object, the NetworkTransform takes care of
            // the synchronization.
            Debug.Log($"Received grabbed Object position: {position}");
            GameObject.transform.SetPositionAndRotation(position, rotation);
        }
    }
}