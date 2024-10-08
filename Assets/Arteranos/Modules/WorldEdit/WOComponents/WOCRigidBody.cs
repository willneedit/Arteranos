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

        private WorldObjectComponent woc = null;
        
        private GameObject GoOrShell => woc?.HasNetworkShell
                    ? woc.HasNetworkShell.gameObject
                    : GameObject;

        public override GameObject GameObject
        {
            get => base.GameObject;
            set
            {
                base.GameObject = value;
                GameObject.TryGetComponent(out woc);
            }
        }

        public Rigidbody Rigidbody 
        { 
            get 
            {
                if (!rigidbody) GameObject.TryGetComponent(out rigidbody);
                return rigidbody;
            } 
        }

        public bool IsKinematic
        {
            get
            {
                if (!isKinematic.HasValue) isKinematic = Rigidbody.isKinematic;
                return isKinematic.Value;
            }
        }

        public bool IsMovable => Grabbable;

        private bool clientGrabbed = false;
        private Rigidbody rigidbody = null;
        private bool? isKinematic = null;

        // Grab & Release: When a non-Host user grabs the object, we have to disable the
        // NetworkTransform to effectively reverse the data flow from the grabbing user's
        // client to the server, which it will propagate to the other users

        public override void Update()
        {
            base.Update();

            // HACK: Value may not set before the first update phase
            bool k = IsKinematic;

            if (clientGrabbed)
            {
                if (G.Me != null) G.Me.GotObjectHeld(GoOrShell, GameObject.transform.position, GameObject.transform.rotation);
                else ServerGotObjectHeld(GameObject.transform.position, GameObject.transform.rotation);
            }
        }

        // On client, suspend the Network Transform to stop receiving data.
        public void GotGrabbed()
        {
            clientGrabbed = true;

            if (woc.HasNetworkShell.TryGetComponent(out ISpawnInitData spawnInitData))
                spawnInitData.SuspendNetworkTransform();

            if (G.Me != null) G.Me.GotObjectGrabbed(GoOrShell);
            else ServerGotGrabbed();
        }

        public void GotReleased()
        {
            clientGrabbed = false;

            if (woc.HasNetworkShell.TryGetComponent(out ISpawnInitData spawnInitData))
                spawnInitData.ResumeNetworkTransform();

            if (G.Me != null) G.Me.GotObjectReleased(GoOrShell);
            else ServerGotReleased();
        }

        // On server, suspend the physics engine for the object to control the movement
        public void ServerGotGrabbed()
        {
            // Either it's already true if we're the host's local user by the XRGrabInteractable,
            // or the Physics interferes with the client's provided tracking. So, set it.
            Rigidbody.isKinematic = true;
        }

        public void ServerGotReleased()
        {
            // XRGrabInteractable would reset the state, but we have to do it manually on
            // the server, especially with the remote controlled object.
            Rigidbody.isKinematic = IsKinematic;
        }

        public void ServerGotObjectHeld(Vector3 position, Quaternion rotation)
        {
            if (woc.HasNetworkShell.TryGetComponent(out ISpawnInitData spawnInitData))
                spawnInitData.PropagateTransform(position, rotation);
            else
                // Transfer the data to the server object, the NetworkTransform takes care of
                // the synchronization.
                GameObject.transform.SetPositionAndRotation(position, rotation);
        }
    }
}