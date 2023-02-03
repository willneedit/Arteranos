using System.Collections;
using UnityEngine;

namespace Arteranos.NetworkIO
{
    public interface IAvatarReplacer
    {
        public Transform LeftHand { get; }
        public Transform RightHand { get; }
        public Transform LeftFoot { get; }
        public Transform RightFoot { get; }

        public Quaternion LhrOffset { get; }
        public Quaternion RhrOffset { get; }


        public Transform CenterEye { get; }
        public Transform Head { get; }

        public abstract void ResetPose();
    }
}