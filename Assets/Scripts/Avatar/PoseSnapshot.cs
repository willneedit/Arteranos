/*
 * Copyright (c) 2015, Unity Technologies
 * Copyright (c) 2019, vis2k, Paul and Contributors
 * Copyright (c) 2023, willneedit
 */

using Mirror;
using UnityEngine;

namespace Arteranos.NetworkIO
{
    // NetworkPose Snapshot
    public struct PoseSnapshot : Snapshot
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
            Debug.Assert(to.rotation.Length == MAX_SIZE);
            Debug.Assert(from.rotation.Length == MAX_SIZE);

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

        public ushort Changed(Quaternion[] last, float rotationSensitivity)
        {
            // last.rotation == null means there's never been a 'last' one.

            if(last == null) return (1 << MAX_SIZE) - 1;

            Debug.Assert(rotation.Length == MAX_SIZE);
            Debug.Assert(last.Length == MAX_SIZE);

            int mask = 0;
            for(int i = 0; i < last.Length; i++)
            {
                if(Quaternion.Angle(last[i], rotation[i]) > rotationSensitivity)
                    mask |= 1 << i;
            }

            return (ushort) mask;
        }
    }
    public static class ExtendPoseSnapshot
    {
        public static void WritePoseSnapshot(this NetworkWriter writer, 
            Quaternion[] quats,
            ref Quaternion[] lastQuats,
            ushort mask, 
            bool compressRotation)
        {
            Debug.Assert(quats.Length == PoseSnapshot.MAX_SIZE);

            writer.WriteUShort(mask);

            for(int i = 0; i < quats.Length; i++)
            {
                if((mask & (1 << i)) != 0)
                {
                    if(compressRotation)
                        writer.WriteUInt(Compression.CompressQuaternion(quats[i]));
                    else
                        writer.WriteQuaternion(quats[i]);

                    // lastQuats are to be only updated when sent, to avoid 'drifting' if
                    // the small changes repeatedly go under the threshold's radar.
                    lastQuats[i] = quats[i];
                }
            }
        }

        public static Quaternion[] ReadPoseSnapshot(this NetworkReader reader, ref Quaternion[] quats, bool compressRotation)
        {
            Debug.Assert(quats.Length == PoseSnapshot.MAX_SIZE);

            ushort mask = reader.ReadUShort();

            for(int i = 0; i < quats.Length; i++)
            {
                if((mask & (1 << i)) != 0)
                {
                    if(compressRotation)
                        quats[i] = Compression.DecompressQuaternion(reader.ReadUInt());
                    else
                        quats[i] = reader.ReadQuaternion();
                }
            }

            return quats;
        }
    }
}
