using UnityEngine;
using System;


namespace Arteranos.ExtensionMethods
{
    using Arteranos.NetworkIO;

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

    }
    public static class ExtendNetworkRotation
    {
        public static NetworkRotation ToNetworkRotation(this Quaternion q)
        {
            Vector3 euler = q.eulerAngles;

            var networkRot = new NetworkRotation();
            networkRot.X = (Int16) (euler.x * 64);
            networkRot.Y = (Int16) (euler.y * 64);
            networkRot.Z = (Int16) (euler.z * 64);
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

    }

}

namespace Arteranos.NetworkIO
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
        public Int16 X;
        public Int16 Y;
        public Int16 Z;

    }
}