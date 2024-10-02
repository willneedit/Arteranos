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
        private WorldObjectComponent woc = null;
        private byte[] serializedSpawnWO = null;

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

        public void GotClicked()
        {
            if (transform.childCount > 0)
            {
                WorldObject spawnWO = transform.GetChild(0).MakeWorldObject();
                using MemoryStream ms = new();
                spawnWO.Serialize(ms);
                serializedSpawnWO = ms.ToArray();
            }
            else if (serializedSpawnWO == null) return;

            // Both the server contains the spawner instances and hold the necessary data.
            SettingsManager.EmitToServerCTSPacket(new CTSObjectSpawn()
            {
                WOSerialized = serializedSpawnWO,
                MaxItems = MaxItems,
                Lifetime = Lifetime,
                Force = Force,
                Position = transform.position, // _World_ coordinates.
                Rotation = transform.rotation,
                spawnerPath = WorldEditorData.GetPathFromObject(transform),
            });
        }
    }
}