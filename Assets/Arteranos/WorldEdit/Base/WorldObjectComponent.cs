/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using System.Collections.Generic;
using ProtoBuf;

using Arteranos.Core;
using UnityEngine;
using System.IO;
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
                SetIsMovable();
            }
        }
        public bool IsCollidable
        {
            get => isCollidable;
            set
            {
                isCollidable = value;
                gameObject.layer = (int) (value ? ColliderType.Solid : ColliderType.Ghostly);
            }
        }
        public bool IsGrabbable
        {
            get => isGrabbable;
            set
            {
                isGrabbable = value;
                SetIsMovable();
            }
        }


        public event Action OnStateChanged;

        private bool isCollidable = false;
        private bool isLocked = false;
        private bool isGrabbable = false;

        Rigidbody body = null;
        XRGrabInteractable mover = null;

        private void Awake()
        {
            body = gameObject.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.drag = 0.5f;
            body.angularDrag = 0.5f;

            mover = gameObject.AddComponent<XRGrabInteractable>();
            mover.throwOnDetach = false;
            mover.smoothPosition = true;
            mover.smoothRotation = true;

            IsCollidable = false;
            IsLocked = false;
        }

        private void Update()
        {
            bool dirty = false;
            foreach(WOCBase w in WOComponents)
            {
                w.Update();
                dirty |= w.Dirty;
            }


            if (dirty) TriggerStateChanged();
        }

        /// <summary>
        /// Notify obserevers about the changed state
        /// </summary>
        public void TriggerStateChanged()
        {
            OnStateChanged?.Invoke();
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
                    WOComponents[i] = wocb;
                    wocb.Awake(gameObject);
                    wocb.CommitState();
                    return true;
                }
            }

            WOComponents.Add(wocb);
            wocb.Awake(gameObject);
            wocb.CommitState();
            return false;
        }

        public void CommitStates()
        {
            foreach(WOCBase w in WOComponents)
                w.CommitState();
        }

        private void SetIsMovable()
        {
            mover.enabled = !IsLocked;
        }

        public WorldObjectPatch MakePatch(bool complete = false)
        {
            WorldObjectPatch wop = new()
            {
                path = new(),
            };

            if (complete)
                wop.components = WOComponents;
            else
            {
                wop.components = new();
                foreach (WOCBase component in WOComponents)
                    if (component.Dirty) wop.components.Add(component);
            }

            Transform current = transform;
            while (current.TryGetComponent(out WorldObjectComponent woc))
            {
                wop.path.Add(woc.Id);
                current = current.parent;
            }
            wop.path.Reverse();

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
                path = new(),
            };

            Transform current = transform.parent;
            while (current.TryGetComponent(out WorldObjectComponent path_woc))
            {
                woi.path.Add(path_woc.Id);
                current = current.parent;
            }
            woi.path.Reverse();

            return woi;
        }

    }
}