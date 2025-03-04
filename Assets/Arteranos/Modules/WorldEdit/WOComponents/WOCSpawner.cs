/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using ProtoBuf;
using System.Collections.Generic;
using UnityEngine;

namespace Arteranos.WorldEdit.Components
{

    [ProtoContract]
    public class WOCSpawner : WOCBase, IClickable
    {
        [ProtoMember(1)]
        public int MaxItems;
        [ProtoMember(2)]
        public WOVector3 Force;
        [ProtoMember(3)]
        public float Lifetime;

        public override void Reset()
        {
            base.Reset();

            MaxItems = 5;
            Force = Vector3.zero;
            Lifetime = 20.0f;
        }

        public void SetState()
        {
            Dirty = true;
        }

        public override object Clone()
        {
            return MemberwiseClone();
        }

        public override void ReplaceValues(WOCBase wOCBase)
        {
            WOCSpawner s = wOCBase as WOCSpawner;

            MaxItems = s.MaxItems;
            Force = s.Force;
            Lifetime = s.Lifetime;
        }

        // ---------------------------------------------------------------

        private Transform transform = null;

        public override GameObject GameObject
        {
            get => base.GameObject;
            set
            {
                base.GameObject = value;
                transform = GameObject.transform;
            }
        }

        public void GotClicked()
        {
            if (G.Me != null) G.Me.GotObjectClicked(WorldEditorData.GetPathFromObject(transform));
            else ServerGotClicked();
        }

        public void ServerGotClicked()
        {
            // If we have no server data storage, create one now. 
            if (!transform.TryGetComponent(out WorldObjectData worldObjectData))
                worldObjectData = transform.gameObject.AddComponent<WorldObjectData>();

            // Check against the max number
            if (worldObjectData.SpawnedItems >= MaxItems) return;

            // All systems go, load the GameObject with the necessary data.

            // Expiring spawned objects decrease the number in the WOComponent's OnDestroy()
            worldObjectData.SpawnedItems++;

            // Filter out the alien gameobjects in the hierarchy and maek down the possible picks
            List<int> picks = new();
            for(int i = 0; i < transform.childCount; i++)
                if(transform.GetChild(i).TryGetComponent(out WorldObjectComponent _)) picks.Add(i);
            if (picks.Count == 0) return;

            G.ArteranosNetworkManager.SpawnObject(new CTSObjectSpawn()
            {
                Pick = picks[Random.Range(0, picks.Count)],
                Lifetime = Lifetime,
                Force = Force,
                Position = transform.position, // _World_ coordinates.
                Rotation = transform.rotation,
                SpawnerPath = WorldEditorData.GetPathFromObject(transform),
            });
        }
    }
}