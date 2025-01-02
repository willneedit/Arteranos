/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using ProtoBuf;
using UnityEngine;

namespace Arteranos.WorldEdit.Components
{
    [ProtoContract]
    public class WOCLight : WOCBase, IPhysicsWOC
    {
        [ProtoMember(1)]
        public WOColor color;

        [ProtoMember(2)]
        public LightType type;

        [ProtoMember(3)]
        public float intensity;

        [ProtoMember(4)]
        public float range;

        [ProtoMember(5)]
        public float angle;


        public override void Reset()
        {
            color = Color.white;
            type = LightType.Spot;
            intensity = 1;
            range = 10;
            angle = 30;
        }

        public void SetState()
        {
            Dirty = true;
        }
        public override object Clone()
        {
            return MemberwiseClone();
        }

        public override void CommitState()
        {
            base.CommitState();

            if (!GameObject) return;

            if (!GameObject.TryGetComponent(out Light light))
                light = GameObject.AddComponent<Light>();

            light.color = color;
            light.type = type;
            light.intensity = intensity;
            light.range = range;
            light.spotAngle = angle;
            light.innerSpotAngle = angle;
            light.shadowAngle = angle;

            //light.lightmapBakeType = LightmapBakeType.Realtime;
        }

        public override void ReplaceValues(WOCBase wOCBase)
        {
            WOCLight light = wOCBase as WOCLight;

            light.color = color;
            light.type = type;
            light.intensity = intensity;
            light.range = range;
            light.angle = angle;
        }

        public void UpdatePhysicsState(bool isInEditMode)
        {
            foreach (Renderer renderer in GameObject.GetComponentsInChildren<Renderer>())
                renderer.enabled = isInEditMode;
        }
    }
}