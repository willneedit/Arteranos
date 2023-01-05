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

    public void PullTransform(Transform t)
    {
        this._position = t.position;
        this._rotation = t.rotation;
        SetDirty(true);
    }

    public void PullLocalTransform(Transform t)
    {
        this._position = t.localPosition;
        this._rotation = t.localRotation;
        SetDirty(true);
    }

    public void PushTransform(Transform t)
    {
        t.SetPositionAndRotation(_position, _rotation);
    }

    public void PushLocalTransform(Transform t)
    {
        t.SetLocalPositionAndRotation(_position, _rotation);
    }
}

[Serializable]
public class TrackedPose {
    public NetworkTrackedBone _self;
    public NetworkTrackedBone leftHand;
    public NetworkTrackedBone rightHand;

}
