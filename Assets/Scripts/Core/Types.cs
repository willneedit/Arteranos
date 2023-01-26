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
            Vector3 euler = q.eulerAngles;

            var networkRot = new NetworkRotation();
            networkRot.X = (short) (euler.x * 64);
            networkRot.Y = (short) (euler.y * 64);
            networkRot.Z = (short) (euler.z * 64);
            return networkRot;
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
    }

}

namespace Arteranos.NetworkTypes
{
    public class NetworkGuid 
    {
        public ulong FirstHalf;
        public ulong SecondHalf;

    }

    /// <summary>
    /// Represents a set of Euler angles down to six bytes payload, precision of the 1/64th of degree.
    /// </summary>
    public class NetworkRotation 
    {
        public short X;
        public short Y;
        public short Z;

    }
}