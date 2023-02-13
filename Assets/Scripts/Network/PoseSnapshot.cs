using Mirror;
using System;
using UnityEngine;

namespace Arteranos.NetworkIO
{
    // NetworkPose Snapshot
    public struct PoseSnapshot : Mirror.Snapshot
    {
        public const int MAX_SIZE = sizeof(ushort) * 8;
        public double remoteTime { get; set; }
        public double localTime { get; set; }

        public Quaternion[] rotation;

        public PoseSnapshot(double remoteTime, double localTime, Quaternion[] rotation)
        {
            this.remoteTime = remoteTime;
            this.localTime = localTime;

            this.rotation = rotation;
        }

        public static PoseSnapshot Interpolate(PoseSnapshot from, PoseSnapshot to, double t)
        {
            Debug.Assert(from.rotation.Length == to.rotation.Length);
            Quaternion[] interpolated = new Quaternion[to.rotation.Length];
            for(int i = 0; i < from.rotation.Length; i++)
                interpolated[i] = Quaternion.SlerpUnclamped(from.rotation[i], to.rotation[i], (float)t);

            return new PoseSnapshot(
                0, 0,
                // IMPORTANT: LerpUnclamped(0, 60, 1.5) extrapolates to ~86.
                //            SlerpUnclamped(0, 60, 1.5) extrapolates to 90!
                //            (0, 90, 1.5) is even worse. for Lerp.
                //            => Slerp works way better for our euler angles.
                interpolated);
        }

        public ushort Changed(PoseSnapshot last, float rotationSensitivity)
        {
            // last.rotation == null means there's never been a 'last' one.

            if(last.rotation == null || 
                last.rotation.Length != rotation.Length) return (ushort) ((1 << MAX_SIZE) - 1);

            Debug.Assert(rotation.Length <= MAX_SIZE);
            int mask = 0;
            for(int i = 0; i < last.rotation.Length; i++)
                if(Quaternion.Angle(last.rotation[i], rotation[i]) > rotationSensitivity)
                    mask |= 1 << i;

            return (ushort) mask;
        }
    }
    public static class ExtendPoseSnapshot
    {
        public static void WritePoseSnapshot(this NetworkWriter writer, Quaternion[] quats, bool compressRotation)
        {
            writer.WriteByte((byte) quats.Length);

            for(int i = 0; i < quats.Length; i++)
            {
                if(compressRotation)
                    writer.WriteUInt(Compression.CompressQuaternion(quats[i]));
                else
                    writer.WriteQuaternion(quats[i]);
            }
        }

        public static Quaternion[] ReadPoseSnapshot(this NetworkReader reader, bool compressRotation)
        {
            int length = reader.ReadByte();

            Debug.Assert(length <= PoseSnapshot.MAX_SIZE);

            Quaternion[] quats = new Quaternion[length];

            for(int i = 0; i < quats.Length; i++)
            {
                if(compressRotation)
                    quats[i] = Compression.DecompressQuaternion(reader.ReadUInt());
                else
                    quats[i] = reader.ReadQuaternion();
            }

            return quats;
        }
    }
}
