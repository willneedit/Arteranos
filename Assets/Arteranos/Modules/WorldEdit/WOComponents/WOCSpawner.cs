/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using ProtoBuf;
using System.IO;
using UnityEngine;

namespace Arteranos.WorldEdit.Components
{

    [ProtoContract]
    public class WOCSpawner : WOCBase, IClickable
    {
        [ProtoMember(1, IsRequired = true)]
        public int MaxItems = 1;
        [ProtoMember(2)]
        public WOVector3 Force;
        [ProtoMember(3, IsRequired = true)]
        public float Lifetime = 10.0f;

        public void SetState()
        {
            Dirty = true;
        }

        public override object Clone()
        {
            return MemberwiseClone();
        }

        public override (string name, GameObject gameObject) GetUI()
            => ("Spawner", BP.I.WorldEdit.SpawnerInspector);

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

            G.ArteranosNetworkManager.SpawnObject(new CTSObjectSpawn()
            {
                Pick = Random.Range(0, transform.childCount),
                Lifetime = Lifetime,
                Force = Force,
                Position = transform.position, // _World_ coordinates.
                Rotation = transform.rotation,
                SpawnerPath = WorldEditorData.GetPathFromObject(transform),
            });
        }
    }
}