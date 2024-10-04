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
using Arteranos.WorldEdit.Components;

namespace Arteranos.WorldEdit
{
    // Just for keeping the data for converting the gameobject to a serializable world object.
    public class WorldObjectComponent : MonoBehaviour
    {
        public WorldObjectAsset Asset { get; set; } = null;
        public Guid Id { get; set; } = new();
        public List<WOCBase> WOComponents { get; set; } = null;
        public DateTime ExpirationTime { get; set; } = DateTime.MaxValue;
        public List<Guid> DataObjectPath { get; set; } = null;
        public bool IsNetworkedObject { get; set; } = false;
        public bool IsNetworkedClientObject
        {
            get => isNetworkedClientObject;
            set
            {
                isNetworkedClientObject = value;
                UpdatePhysicsState();
            }
        }
        public bool IsLocked
        {
            get => isLocked;
            set
            {
                isLocked = value;
                UpdatePhysicsState();
            }
        }

        public IClickable IsClickable
        {
            get
            {
                foreach (WOCBase component in WOComponents)
                    if (component is IClickable cl) return cl;
                return null;
            }
        }

        public IRigidBody IsGrabbable
        {
            get
            {
                foreach (WOCBase component in WOComponents)
                    if (component is IRigidBody gr) return gr;
                return null;
            }
        }


        private bool isNetworkedClientObject = false;
        private bool isLocked = false;

        private Rigidbody body = null;
        private XRGrabInteractable mover = null;
        private XRSimpleInteractable clicker = null;

        private void Awake()
        {
            body = gameObject.AddComponent<Rigidbody>();

            mover = gameObject.AddComponent<XRGrabInteractable>();
            mover.throwOnDetach = false;
            mover.smoothPosition = true;
            mover.smoothRotation = true;
            mover.enabled = false;

            clicker = gameObject.AddComponent<XRSimpleInteractable>();
            clicker.enabled = false;

            mover.firstSelectEntered.AddListener(GotObjectGrabbed);
            mover.lastSelectExited.AddListener(GotObjectReleased);

            clicker.activated.AddListener(GotObjectClicked);

            G.WorldEditorData.OnEditorModeChanged += GotEditorModeChanged;

            UpdatePhysicsState();
        }

        private void OnDestroy()
        {
            G.WorldEditorData.OnEditorModeChanged -= GotEditorModeChanged;

            // If it's a spawned object, sign ourselves off. No matter if clients miscounted,
            // the server has the authority.
            if (IsNetworkedObject && DataObjectPath != null)
            {
                try
                {
                    // throws if the data storage is already been destroyed, but that would be okay
                    Transform doT = WorldEditorData.FindObjectByPath(DataObjectPath);
                    if (doT != null && doT.TryGetComponent(out WorldObjectData worldObjectData))
                        worldObjectData.SpawnedItems--;
                }
                catch { }
            }
        }

        private void GotEditorModeChanged(bool editing) => UpdatePhysicsState();

        private void Update()
        {
            if (ExpirationTime < DateTime.UtcNow)
            {
                // It it's networked, target the shell instead of the object itself.
                Transform toGoT = IsNetworkedObject ? transform.parent : transform;

                // And if it's networked, let the server do it, not the client itself.
                // togoT can be null if the object is currently held - they will be
                // expired as soon as the user releases it.
                if (!isNetworkedClientObject && toGoT)
                    Destroy(toGoT.gameObject);
            }
                
            foreach (WOCBase component in WOComponents)
                component.Update();
        }

        private void GotObjectGrabbed(SelectEnterEventArgs arg0)
        {
            // Grabbing means it's the focused object.
            if (!G.WorldEditorData.IsInEditMode)
                IsGrabbable?.GotGrabbed();
            else
                G.WorldEditorData.FocusedWorldObject = gameObject;
        }

        private void GotObjectReleased(SelectExitEventArgs arg0)
        {
            if (!G.WorldEditorData.IsInEditMode)
                IsGrabbable?.GotReleased();
            else
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

        private void GotObjectClicked(ActivateEventArgs arg0)
        {
            if (!G.WorldEditorData.IsInEditMode) 
                IsClickable?.GotClicked();
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

        public bool TryGetWOC<T>(out T woComponent) where T : class
        {
            foreach (object w in WOComponents)
                if (w is T woc)
                {
                    woComponent = woc;
                    return true;
                }
            woComponent = null;
            return false;
        }

        public bool TryGetWOC(out WOCBase woComponent, Type t)
        {
            foreach (WOCBase w in WOComponents)
                if (w.GetType() == t)
                {
                    woComponent = w;
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

            bool isInEditMode = G.WorldEditorData.IsInEditMode;
            bool needsKinematic = IsNetworkedClientObject;

            // It's in a not (yet) instantiated object, take it as-is within CommitState()
            if (!body || !mover || !clicker) return;

            // Meta objects are inactive out of the edit mode
            if(TryGetWOC<IMetaObject>(out _))
                gameObject.SetActive(isInEditMode);


            // Same as with the *parent* being the spawner object.
            if(transform.parent != null)
            {
                WorldObjectComponent parentWOC = transform.parent.GetComponent<WorldObjectComponent>();
                if (parentWOC != null && parentWOC.TryGetWOC<WOCSpawner>(out _))
                    gameObject.SetActive(isInEditMode);
            }
            // Disable both of them to prevent the warning about conflicting interactables
            mover.enabled = false;
            clicker.enabled = false;

            // Fixup - Unity yells at you if it is a concave collider and it's physics-controlled.
            MeshCollider[] colliders = gameObject.GetComponentsInChildren<MeshCollider>();
            foreach (MeshCollider collider in colliders)
                needsKinematic |= !collider.convex;

            body.isKinematic = needsKinematic;

            TryGetWOC(out WOCTransform t);
            TryGetWOC(out WOCRigidBody rb);

            // Prevent physics shenanigans in the edit mode
            body.constraints = isInEditMode
                ? RigidbodyConstraints.FreezeAll
                : RigidbodyConstraints.None;

            body.useGravity = !isInEditMode
                && (rb?.ObeysGravity ?? false);
            body.mass = rb?.Mass ?? 0;
            body.drag = rb?.Drag ?? 0;
            body.angularDrag = rb?.AngularDrag ?? 0;

            RecursiveSetLayer((int)(t?.isCollidable ?? false
                ? ColliderType.Solid
                : ColliderType.Intangible), transform);           

            // #153: If we're in edit mode in desktop, lock rotation in grab
            mover.trackRotation = !(G.WorldEditorData.IsInEditMode && !G.Client.VRMode);

            mover.enabled = isInEditMode
                ? !IsLocked
                : IsGrabbable?.IsMovable ?? false;

            clicker.enabled = !isInEditMode && (IsClickable != null);
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