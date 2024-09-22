/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */


using System.Collections.Generic;

using UnityEngine;
using System;
using UnityEngine.XR.Interaction.Toolkit;

namespace Arteranos.WorldEdit
{
    // Just for keeping the data for converting the gameobject to a serializable world object.
    public class WorldObjectComponent : MonoBehaviour
    {
        public WorldObjectAsset Asset { get; set; } = null;
        public Guid Id { get; set; } = new();
        public List<WOCBase> WOComponents { get; set; } = null;
        public bool IsLocked
        {
            get => isLocked;
            set
            {
                isLocked = value;
                UpdatePhysicsState();
            }
        }

        private bool isLocked = false;

        private Rigidbody body = null;
        private XRGrabInteractable mover = null;

        private void Awake()
        {
            body = gameObject.AddComponent<Rigidbody>();

            mover = gameObject.AddComponent<XRGrabInteractable>();
            mover.throwOnDetach = false;
            mover.smoothPosition = true;
            mover.smoothRotation = true;

            mover.firstSelectEntered.AddListener(GotObjectGrabbed);
            mover.lastSelectExited.AddListener(GotObjectReleased);

            G.WorldEditorData.OnEditorModeChanged += GotEditorModeChanged;

            UpdatePhysicsState();
        }

        private void OnDestroy()
        {
            G.WorldEditorData.OnEditorModeChanged -= GotEditorModeChanged;
        }

        private void GotEditorModeChanged(bool editing) => UpdatePhysicsState();

        private void Update()
        {
            foreach (WOCBase component in WOComponents)
                component.Update();
        }

        private void GotObjectReleased(SelectExitEventArgs arg0)
        {
            if(G.WorldEditorData.IsInEditMode)
            {
                // We're in edit mode, we are actually changing the world.
                TryGetWOC(out WOCTransform woct);

                (Vector3 position, Vector3 eulerRotation) = ConstrainMovement(woct.position, woct.rotation);

                woct.SetState(
                    position, 
                    eulerRotation, 
                    woct.scale);

                WorldObjectPatch wop = new() { components = new() { woct } };
                wop.SetPathFromThere(transform);
                wop.EmitToServer();
            }
        }

        private void GotObjectGrabbed(SelectEnterEventArgs arg0)
        {
            // Grabbing means it's the focused object.
            if(G.WorldEditorData.IsInEditMode)
                G.WorldEditorData.FocusedWorldObject = gameObject;
        }



        private (Vector3 position, Vector3 eulerRotation) ConstrainMovement(Vector3 oldPosition, Vector3 oldEulerRotation)
        {
            if(!G.WorldEditorData.LockXAxis && !G.WorldEditorData.LockYAxis && !G.WorldEditorData.LockZAxis)
                return (transform.localPosition, transform.localEulerAngles);

            Vector3 position = transform.localPosition;
            Vector3 eulerRotation = transform.localEulerAngles;

            if(G.WorldEditorData.LockXAxis)
            {
                position.x = oldPosition.x;
                eulerRotation.x = oldEulerRotation.x;
            }

            if(G.WorldEditorData.LockYAxis)
            {
                position.y = oldPosition.y;
                eulerRotation.y = oldEulerRotation.y;
            }

            if(G.WorldEditorData.LockZAxis)
            {
                position.z = oldPosition.z;
                eulerRotation.z = oldEulerRotation.z;
            }

            return (position, eulerRotation);
        }


        public bool TryGetWOC<T>(out T woComponent) where T : WOCBase
        {
            foreach (WOCBase w in WOComponents)
                if(w is T woc)
                {
                    woComponent = woc;
                    return true;
                }
            woComponent = null;
            return false;
        }

        public bool AddOrReplaceComponent(WOCBase wocb)
        {
            for (int i = 0; i < WOComponents.Count; i++)
            {
                WOCBase w = WOComponents[i];
                if (wocb.GetType() == w.GetType())
                {
                    w.ReplaceValues(wocb);
                    w.CommitState();
                    return true;
                }
            }

            WOComponents.Add(wocb);
            wocb.GameObject = gameObject;
            wocb.CommitState();
            return false;
        }

        public void CommitStates()
        {
            foreach(WOCBase w in WOComponents)
                w.CommitState();
        }

        public void UpdatePhysicsState()
        {
            static void RecursiveSetLayer(int layer, Transform t)
            {
                t.gameObject.layer = layer;
                for(int i = 0; i < t.childCount; i++)
                    RecursiveSetLayer(layer, t.GetChild(i));
            }

            // It's in a not (yet) instantiated object, take it as-is within CommitState()
            if (!body || !mover) return;

            // Fixup - Unity yells at you if it is a concave collider and it's physics-controlled.
            bool needsKinematic = false;
            MeshCollider[] colliders = gameObject.GetComponentsInChildren<MeshCollider>();
            foreach (MeshCollider collider in colliders)
                needsKinematic |= !collider.convex;

            body.isKinematic = needsKinematic;

            TryGetWOC(out WOCTransform t);
            TryGetWOC(out WOCPhysics p);

            // Prevent physics shenanigans in the edit mode
            bool isInEditMode = G.WorldEditorData.IsInEditMode;
            body.constraints = isInEditMode
                ? RigidbodyConstraints.FreezeAll
                : RigidbodyConstraints.None;

            body.useGravity = !isInEditMode
                && (p?.ObeysGravity ?? false);

            RecursiveSetLayer((int)(t?.isCollidable ?? false
                ? ColliderType.Solid
                : ColliderType.Intangible), transform);           

            mover.enabled = isInEditMode
                ? !IsLocked
                : p?.Grabbable ?? false;

            // #153: If we're in edit mode in desktop, lock rotation in grab
            mover.trackRotation = !(G.WorldEditorData.IsInEditMode && !G.Client.VRMode);
        }

        public WorldObjectPatch MakePatch(bool complete = false)
        {
            WorldObjectPatch wop = new();

            if (complete)
                wop.components = WOComponents;
            else
            {
                wop.components = new();
                foreach (WOCBase component in WOComponents)
                    if (component.Dirty) wop.components.Add(component);
            }

            wop.SetPathFromThere(transform);
            return wop;
        }

        public WorldObjectInsertion MakeInsertion()
        {
            transform.TryGetComponent(out WorldObjectComponent woc);
            WorldObjectInsertion woi = new()
            {
                asset = woc.Asset,
                components = woc.WOComponents,
                name = transform.name,
                id = Guid.NewGuid(), // Creating a copy of an existing one, so spawn a new guid
            };

            woi.SetPathFromThere(transform.parent);
            return woi;
        }

    }
}