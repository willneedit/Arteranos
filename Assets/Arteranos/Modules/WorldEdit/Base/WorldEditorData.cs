/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using System.Collections.Generic;
using ProtoBuf;

using UnityEngine;
using System.IO;
using System;


namespace Arteranos.WorldEdit
{
    public class WorldEditorData : MonoBehaviour, IWorldEditorData
    {
        struct UndoBuffer
        {
            public string hash;
            public List<byte[]> SerializedWorldObjects;
        }

        // Movement and rotation constraints
        public bool LockXAxis { get => lockXAxis; set => lockXAxis = value; }
        public bool LockYAxis { get => lockYAxis; set => lockYAxis = value; }
        public bool LockZAxis { get => lockZAxis; set => lockZAxis = value; }

        [SerializeField] private bool isInEditMode = false;
        [SerializeField] private bool lockXAxis = false;
        [SerializeField] private bool lockYAxis = false;
        [SerializeField] private bool lockZAxis = false;

        // Are we in the edit mode at all?
        public bool IsInEditMode
        {
            get => isInEditMode;
            set
            {
                bool old = isInEditMode;
                isInEditMode = value;
                if (old != value) NotifyEditorModeChanged();
            }
        }

        public event Action<IWorldChange> OnWorldChanged;
        public event Action<bool> OnEditorModeChanged;

        public void NotifyWorldChanged(IWorldChange worldChange)
            => OnWorldChanged?.Invoke(worldChange);

        public void NotifyEditorModeChanged()
            => OnEditorModeChanged?.Invoke(IsInEditMode);

        private List<UndoBuffer> undoStack = new();
        private int undoCount = 0;
        private readonly Dictionary<IWorldObjectAsset, GameObject> AssetBlueprints = new();

        private void Awake()
        {
            G.WorldEditorData = this;
        }

        private void OnDestroy()
        {
            ClearBlueprints();
        }

        private IEnumerable<WorldObject> MakeSnapshot()
        {
            Transform t = WorldChange.FindObjectByPath(null);
            for (int i = 0; i < t.childCount; i++)
            {
                WorldObject wo = t.GetChild(i).MakeWorldObject();
                if (wo != null) yield return wo;
            }
        }

        private void AddUndoEntry()
        {
            UndoBuffer buffer = new()
            {
                SerializedWorldObjects = new()
            };


            // Cut off the undo stack, discarding the more recent snapshots
            if (undoCount != 0)
            {
                undoStack = undoStack.GetRange(0, undoStack.Count - undoCount);
                undoCount = 0;
            }

            // Has to be reproducible across all clients, so no GUID.NewGuid()...
            Hash128 hash = new();

            // And now, take a new snapshot as the most recent one
            foreach (WorldObject wo in MakeSnapshot())
            {
                using MemoryStream ms = new();
                wo.Serialize(ms);
                byte[] item = ms.ToArray();
                hash.Append(item);
                buffer.SerializedWorldObjects.Add(item);
            }
            buffer.hash = hash.ToString();

            undoStack.Add(buffer);
        }

        private void EmitUndoRedo()
        {
            WorldRollbackRequest request = new()
            {
                // Has to be same across all clients, or they'd be desynced.
                hash = undoStack[^undoCount].hash,
            };
            request.EmitToServer();
        }

        public IEnumerator RecallUndoState(string hash)
        {
            static IEnumerator Cor(List<byte[]> serialized)
            {
                Transform t = WorldChange.FindObjectByPath(null);

                // First, try to remember the previous gameobjects.
                List<Transform> old = new();
                for (int i = 0; i < t.childCount; i++) old.Add(t.GetChild(i));

                // And, let them fall under the radar.
                t.DetachChildren();

                // Rebuild the state from the undo buffer
                foreach (byte[] ser in serialized)
                {
                    using MemoryStream ms = new(ser);
                    WorldObject wo = WorldObject.Deserialize(ms);
                    yield return wo.Instantiate(t);
                }

                // And now, destroy the remembered gameobjects.
                foreach (Transform item in old)
                    Destroy(item.gameObject);
            }

            UndoBuffer? found = null;
            foreach(var item in undoStack)
                if(item.hash == hash)
                {
                    found = item; break;
                }

            // Maybe it is a latecomer who hasn't the undo state that far back, or
            // he's desynced.
            if (found == null) yield break;

            yield return Cor(found.Value.SerializedWorldObjects);
        }

        public void DoApply(IWorldChange worldChange)
        {
            IEnumerator Cor()
            {
                if (worldChange is not WorldRollbackRequest)
                    AddUndoEntry();

                // Make the changes real.
                // Rollback request will recall an arbitrary snapshot from the stack.
                yield return worldChange.Apply();

                NotifyWorldChanged(worldChange);
            }

            StartCoroutine(Cor());
        }

        public void BuilderRequestsUndo()
        {
            // Before navigating back and forth through the undo stack, we need to add the _current_ statw
            // on top of the stack.
            if(undoCount == 0)
            {
                AddUndoEntry();
                undoCount = 1;
            }

            if (undoCount >= undoStack.Count) return;
            undoCount++;

            EmitUndoRedo();
        }

        public void BuilderRequestedRedo()
        {
            if (undoCount <= 1) return;
            undoCount--;

            EmitUndoRedo();
        }

        public void DoApply(Stream stream)
        {
            IWorldChange worldChange = WorldChange.Deserialize(stream);
            DoApply(worldChange);
        }

        public IWorldDecoration TakeSnapshot()
        {
            WorldDecoration worldDecoration = new() 
            { 
                info = null, 
                objects = new() 
            };
            worldDecoration.TakeSnapshot();
            return worldDecoration;
        }

        public IEnumerator BuildWorld(IWorldDecoration worldDecoration)
            => worldDecoration.BuildWorld();

        public IWorldDecoration DeserializeWD(Stream stream)
            => Serializer.Deserialize<WorldDecoration>(stream);

        public void ClearBlueprints()
        {
            foreach(var bp in AssetBlueprints)
                Destroy(bp.Value);

            AssetBlueprints.Clear();
        }

        public bool TryGetBlueprint(IWorldObjectAsset woa, out GameObject gameObject) 
            => AssetBlueprints.TryGetValue(woa, out gameObject);

        public void AddBlueprint(IWorldObjectAsset woa, GameObject gameObject) 
            => AssetBlueprints.Add(woa, gameObject);
    }
}