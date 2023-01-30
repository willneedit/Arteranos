using UnityEngine;
using System;

using Mirror;

namespace Arteranos.ExtensionMethods
{
    using Arteranos.NetworkTypes;

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

    public static class ExtendNetworkGuid
    {
        
        public static NetworkGuid ToNetworkGuid(this Guid id)
        {
            var networkId = new NetworkGuid();
            networkId.FirstHalf = BitConverter.ToUInt64(id.ToByteArray(), 0);
            networkId.SecondHalf = BitConverter.ToUInt64(id.ToByteArray(), 0);
            return networkId;
        }

        public static Guid ToGuid(this NetworkGuid networkId)
        {
            var bytes = new byte[16];
            Buffer.BlockCopy(BitConverter.GetBytes(networkId.FirstHalf), 0, bytes, 0, 8);
            Buffer.BlockCopy(BitConverter.GetBytes(networkId.SecondHalf), 0, bytes, 8, 8);
            return new Guid(bytes);
        }

        public static void WriteNetworkGuid(this NetworkWriter writer, NetworkGuid value)
        {
            writer.WriteULong(value.FirstHalf);
            writer.WriteULong(value.SecondHalf);
        }

        public static NetworkGuid ReadNetworkGuid(this NetworkReader reader)
        {
            var res = new NetworkGuid();
            res.FirstHalf = reader.ReadULong();
            res.SecondHalf = reader.ReadULong();
            return res;
        }

    }
    public static class ExtendNetworkRotation
    {
        public static NetworkRotation ToNetworkRotation(this Quaternion q)
        {
            var networkRot = new NetworkRotation();
            q.ToNetworkRotation(ref networkRot);
            return networkRot;
        }

        public static void ToNetworkRotation(this Quaternion q, ref NetworkRotation netr)
        {
            Vector3 euler = q.eulerAngles;

            netr.X = (short)(euler.x * 64);
            netr.Y = (short)(euler.y * 64);
            netr.Z = (short)(euler.z * 64);
        }

        public static Quaternion ToQuaternion(this NetworkRotation networkRot)
        {
            return Quaternion.Euler(
                ((float) networkRot.X) / 64.0f,
                ((float) networkRot.Y) / 64.0f,
                ((float) networkRot.Z) / 64.0f
            );
        }

        public static void WriteNetworkRotation(this NetworkWriter writer, NetworkRotation value)
        {
            writer.WriteShort(value.X);
            writer.WriteShort(value.Y);
            writer.WriteShort(value.Z);
        }

        public static NetworkRotation ReadNetworkRotation(this NetworkReader reader)
        {
            var res = new NetworkRotation();
            res.X = reader.ReadShort();
            res.Y = reader.ReadShort();
            res.Z = reader.ReadShort();
            return res;
        }

        /// <summary>
        /// In-place reading of the serialized data, to avoid poltergeists
        /// </summary>
        /// <param name="reader">The reader</param>
        /// <param name="res">The NetworkRotation to overwrite to</param>
        public static void ReadNetworkRotation(this NetworkReader reader, ref NetworkRotation res)
        {
            res.X = reader.ReadShort();
            res.Y = reader.ReadShort();
            res.Z = reader.ReadShort();
        }

    }

}

namespace Arteranos.NetworkTypes
{
    using Arteranos.ExtensionMethods;

    public class NetworkGuid 
    {
        public ulong FirstHalf;
        public ulong SecondHalf;

    }

    /// <summary>
    /// Represents a set of Euler angles down to six bytes payload, precision of the 1/64th of degree.
    /// </summary>
    public class NetworkRotation : IEquatable<NetworkRotation>
    {
        public short X;
        public short Y;
        public short Z;

        public NetworkRotation(short _X = 0, short _Y = 0, short _Z = 0)
        {
            X = _X; Y = _Y; Z = _Z;
        }

        public bool Equals(NetworkRotation other)
        {
            return other.X == X && other.Y == Y && other.Z == Z;
        }
    }

    public class SyncPose : SyncObject
    {
        public const int MAX_JOINTS = 16;

        private ushort m_JointDirtyBits;
        private NetworkRotation[] m_Joint;
        private bool hookGuard = false;

        public SyncPose()
        {
            Reset();
        }

        public override void Reset()
        {            
            this.m_Joint = new NetworkRotation[MAX_JOINTS];
            for(int i = 0;i < MAX_JOINTS; i++)
                this.m_Joint[i] = new NetworkRotation();
            
            m_JointDirtyBits = 0;
        }

        public override void ClearChanges()
        {
            m_JointDirtyBits = 0;
        }

        public override void OnSerializeAll(NetworkWriter writer)
        {
            ushort allBits = (1 << MAX_JOINTS) - 1;

            writer.WriteUShort(allBits);
            for(int i = 0, m = 1; i < MAX_JOINTS; i++, m = m << 1)
                if((allBits & m)!= 0) writer.WriteNetworkRotation(m_Joint[i]);
        }

        public override void OnSerializeDelta(NetworkWriter writer)
        {
            writer.WriteUShort(m_JointDirtyBits);
            for(int i = 0, m = 1; i < MAX_JOINTS; i++, m = m << 1)
                if((m_JointDirtyBits & m)!= 0) writer.WriteNetworkRotation(m_Joint[i]);
        }

        public override void OnDeserializeAll(NetworkReader reader)
        {
            OnDeserializeDelta(reader);
        }

        public override void OnDeserializeDelta(NetworkReader reader)
        {
            m_JointDirtyBits = reader.ReadUShort();
            for(int i = 0, m = 1; i < MAX_JOINTS; i++, m = m << 1)
                if((m_JointDirtyBits & m)!= 0)
                {
                    reader.ReadNetworkRotation(ref m_Joint[i]);
                    InvokeCallback(i);
                }
        }

        public NetworkRotation this[int index]
        {
            get => m_Joint[index];
            set {
                if(!m_Joint[index].Equals(value))
                {
                    m_Joint[index] = value;
                    OnDirty();
                    m_JointDirtyBits = (ushort) (m_JointDirtyBits | (1 << index));

                    if(!hookGuard && NetworkClient.active)
                    {
                        hookGuard = true;
                        InvokeCallback(index);
                        hookGuard = false;
                    }
                }
            }
        }

        public void UpdateJoint(int index, Quaternion q)
        {

        }

        public event Action<int> Callback;

        protected virtual void InvokeCallback(int index) => Callback?.Invoke(index);
    }
}