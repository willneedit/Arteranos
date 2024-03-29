/*
 * Copyright (c) 2015, Unity Technologies
 * Copyright (c) 2019, vis2k, Paul and Contributors
 * Copyright (c) 2023, willneedit
 */

using System.Collections.Generic;
using UnityEngine;

using Mirror;
using Arteranos.Core;

namespace Arteranos.NetworkIO
{
    public abstract class NetworkPoseBase : NetworkBehaviour
    {
        protected bool IsClientWithAuthority => isClient && authority;
        public readonly SortedList<double, PoseSnapshot> clientSnapshots = new();
        public readonly SortedList<double, PoseSnapshot> serverSnapshots = new();

        public string[] jointNames = null;
        public Transform[] jointTransforms = null;

        protected Animator anim = null;

        protected virtual void Awake()
        {
            jointNames = new string[PoseSnapshot.MAX_SIZE];
            jointTransforms = new Transform[PoseSnapshot.MAX_SIZE];
        }

        public virtual void UploadJointNames(Transform rootTransform, string[] names)
        {
            Debug.Assert(names.Length <= PoseSnapshot.MAX_SIZE);

            jointNames = new string[PoseSnapshot.MAX_SIZE];
            jointTransforms = new Transform[PoseSnapshot.MAX_SIZE];

            for(int i = 0; i < names.Length; i++)
            {
                jointNames[i] = names[i];
                jointTransforms[i] = rootTransform.FindRecursive(names[i]);
                Debug.Assert(jointTransforms[i] != null);
            }


            // The user got a new body in general, even if it's not yet active.
            anim = GetComponentInChildren<Animator>(true);
        }

        public Quaternion[] RetrievePoseData()
        {
            Quaternion[] rotations = new Quaternion[jointTransforms.Length];

            for(int i = 0; i < jointTransforms.Length; i++)
            {
                if(jointTransforms[i] != null)
                    rotations[i] = jointTransforms[i].transform.localRotation;
            }

            return rotations;
        }

        protected virtual void OnValidate()
        {
            syncDirection = SyncDirection.ClientToServer;
            syncInterval = 0;
        }

        protected virtual PoseSnapshot Construct()
        {
            return new PoseSnapshot(
                Time.timeAsDouble,
                0, // the other end fills out local time itself
                RetrievePoseData()
            );
        }

        protected void AddSnapshot(SortedList<double, PoseSnapshot> snapshots, double timeStamp, Quaternion[] rotation)
        {
            SnapshotInterpolation.InsertIfNotExists(snapshots, new PoseSnapshot(
                timeStamp, // arrival remote timestamp. NOT remote time.
                Time.timeAsDouble,
                rotation
            ));
        }

        protected virtual void Apply(PoseSnapshot interpolated)
        {
            Quaternion[] data = interpolated.rotation;

            for(int i = 0; i < jointTransforms.Length; i++)
            {
                if(jointTransforms[i] != null)
                    jointTransforms[i].transform.localRotation = data[i];
            }
        }

        public virtual void Reset()
        {
            serverSnapshots.Clear();
            clientSnapshots.Clear();
        }

        protected virtual void OnDisable() => Reset();
        protected virtual void OnEnable()  => Reset();

    }
}
