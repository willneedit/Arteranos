using Unity.Netcode;
using UnityEngine;
using System;

[Serializable]
public class NetworkTrackedBone : NetworkVariableBase {
    [SerializeField]
    private Vector3 _position = Vector3.zero;

    [SerializeField]
    private Quaternion _rotation = Quaternion.identity;

    public Vector3 position {
        get => _position;
        set { _position = value; SetDirty(true); }
    }

    public Quaternion rotation {
        get => _rotation;
        set { _rotation = value; SetDirty(true); }
    }

    public override void WriteField(FastBufferWriter writer)
    {
        writer.WriteValueSafe(_position);
        writer.WriteValueSafe(_rotation);
    }

    public override void ReadField(FastBufferReader reader)
    {
        reader.ReadValueSafe(out _position);
        reader.ReadValueSafe(out _rotation);
    }

    public override void WriteDelta(FastBufferWriter writer)
    {
        writer.WriteValueSafe(_position);
        writer.WriteValueSafe(_rotation);
    }

    public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
    {
        reader.ReadValueSafe(out _position);
        reader.ReadValueSafe(out _rotation);
    }

    public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
    {
        this._position = position;
        this._rotation = rotation;
        SetDirty(true);
    }

}

[Serializable]
public class TrackedPose {
    public NetworkTrackedBone _self;
    public NetworkTrackedBone leftHand;
    public NetworkTrackedBone rightHand;

}

namespace Arteranos.ExtensionMethods
{
    public static class ExtendTransform
    {
        public static Transform FindRecursive(this Transform t, string name)
        {
            if(t.name == name) return t;

            for(int i = 0, c = t.childCount; i<c; i++)
            {
                Transform res = FindRecursive(t.GetChild(i), name);
                if(res != null) return res;
            }

            return null;
        }
    }
}