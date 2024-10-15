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

        public void GotReleased()
        {
            if (G.Me != null)
            {
                G.Me.GotObjectReleased(GameObject);
                G.Me.ManageAuthorityOf(GameObject, false);
            }
            else ServerGotReleased();
        }

        // On server, suspend the physics engine for the object to control the movement
        public void ServerGotGrabbed() { }

        public void ServerGotReleased() { }

        public void ServerGotObjectHeld(Vector3 position, Quaternion rotation) { }
    }
}