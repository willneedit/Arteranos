using System.Collections.Generic;
using UnityEngine;

using Mirror;

namespace Arteranos.NetworkIO
{
    public class NetworkPose : NetworkPoseBase
    {
        [Header("Sync Only If Changed")]
        [Tooltip("When true, changes are not sent unless greater than sensitivity values below.")]
        public bool onlySyncOnChange = true;
        [Tooltip("If we only sync on change, then we need to correct old snapshots if more time than sendInterval * multiplier has elapsed.\n\nOtherwise the first move will always start interpolating from the last move sequence's time, which will make it stutter when starting every time.")]
        public float onlySyncOnChangeCorrectionMultiplier = 2;

        [Header("Rotation")]
        [Tooltip("Sensitivity of changes needed before an updated state is sent over the network")]
        public float rotationSensitivity = 0.01f;
        [Tooltip("Apply smallest-three quaternion compression. This is lossy, you can disable it if the small rotation inaccuracies are noticeable in your project.")]
        public bool compressRotation = false;

        // Used to store last sent snapshots
        protected PoseSnapshot last;

        protected virtual bool Changed(PoseSnapshot current) =>
            current.Changed(last.rotation, rotationSensitivity) != 0;

        // only sync on change /////////////////////////////////////////////////
        // snap interp. needs a continous flow of packets.
        // 'only sync on change' interrupts it while not changed.
        // once it restarts, snap interp. will interp from the last old position.
        // this will cause very noticeable stutter for the first move each time.
        // the fix is quite simple.

        // 1. detect if the remaining snapshot is too old from a past move.
        static bool NeedsCorrection(
            SortedList<double, PoseSnapshot> snapshots,
            double remoteTimestamp,
            double bufferTime,
            double toleranceMultiplier) =>
                snapshots.Count == 1 &&
                remoteTimestamp - snapshots.Keys[0] >= bufferTime * toleranceMultiplier;

        // 2. insert a fake snapshot at current position,
        //    exactly one 'sendInterval' behind the newly received one.
        static void RewriteHistory(
            SortedList<double, PoseSnapshot> snapshots,
            // timestamp of packet arrival, not interpolated remote time!
            double remoteTimeStamp,
            double localTime,
            double sendInterval,
            Quaternion[] rotation)
        {
            // clear the previous snapshot
            snapshots.Clear();

            // insert a fake one at where we used to be,
            // 'sendInterval' behind the new one.
            SnapshotInterpolation.InsertIfNotExists(snapshots, new PoseSnapshot(
                remoteTimeStamp - sendInterval, // arrival remote timestamp. NOT remote time.
                localTime - sendInterval,       // Unity 2019 doesn't have timeAsDouble yet
                rotation
            ));
        }

        // sync ////////////////////////////////////////////////////////////////

        // local authority client sends sync message to server for broadcasting
        protected virtual void OnClientToServerSync(Quaternion[] rotation)
        {
            // only apply if in client authority mode
            if (syncDirection != SyncDirection.ClientToServer) return;

            // protect against ever growing buffer size attacks
            if (serverSnapshots.Count >= connectionToClient.snapshotBufferSizeLimit) return;

            // 'only sync on change' needs a correction on every new move sequence.
            if (onlySyncOnChange &&
                NeedsCorrection(serverSnapshots, connectionToClient.remoteTimeStamp, NetworkServer.sendInterval, onlySyncOnChangeCorrectionMultiplier))
            {
                RewriteHistory(
                    serverSnapshots,
                    connectionToClient.remoteTimeStamp,
                    NetworkTime.localTime,      // arrival remote timestamp. NOT remote timeline.
                    NetworkServer.sendInterval, // Unity 2019 doesn't have timeAsDouble yet
                    RetrievePoseData());
                // Debug.Log($"{name}: corrected history on server to fix initial stutter after not sending for a while.");
            }

            AddSnapshot(serverSnapshots, connectionToClient.remoteTimeStamp, rotation);
        }

        // server broadcasts sync message to all clients
        protected virtual void OnServerToClientSync(Quaternion[] rotation)
        {
            // don't apply for local player with authority
            if (IsClientWithAuthority) return;

            // 'only sync on change' needs a correction on every new move sequence.
            if (onlySyncOnChange &&
                NeedsCorrection(clientSnapshots, NetworkClient.connection.remoteTimeStamp, NetworkClient.sendInterval, onlySyncOnChangeCorrectionMultiplier))
            {
                RewriteHistory(
                    clientSnapshots,
                    NetworkClient.connection.remoteTimeStamp, // arrival remote timestamp. NOT remote timeline.
                    NetworkTime.localTime,                    // Unity 2019 doesn't have timeAsDouble yet
                    NetworkClient.sendInterval,
                    RetrievePoseData());
                // Debug.Log($"{name}: corrected history on client to fix initial stutter after not sending for a while.");
            }

            AddSnapshot(clientSnapshots, NetworkClient.connection.remoteTimeStamp, rotation);
        }

        bool SkipQueue() =>
            isServer &&
            syncDirection == SyncDirection.ClientToServer &&
            serverSnapshots.Count > 0;

        Quaternion[] lastSerializedRotations = null;

        public override void OnSerialize(NetworkWriter writer, bool initialState)
        {
            // get current snapshot for broadcasting.
            PoseSnapshot snapshot = Construct();

            // ClientToServer optimization:
            // for interpolated client owned identities,
            // always broadcast the latest known snapshot so other clients can
            // interpolate immediately instead of catching up too
            if (SkipQueue())
            {
                snapshot = serverSnapshots.Values[serverSnapshots.Count - 1];
                // Debug.Log($"Skipped snapshot queue for {name} to snapshot[{serverSnapshots.Count-1}]");
            }

            ushort mask;
            if(initialState || lastSerializedRotations == null)
            {
                // initial - serialize all of these
                mask = (1 << PoseSnapshot.MAX_SIZE) - 1;
                lastSerializedRotations = new Quaternion[PoseSnapshot.MAX_SIZE];
            }
            else // ... and delta
                mask = snapshot.Changed(lastSerializedRotations, rotationSensitivity);

            writer.WritePoseSnapshot(snapshot.rotation, ref lastSerializedRotations, mask, compressRotation);

            // set 'last'
            last = snapshot;
        }

        Quaternion[] lastDeserializedRotations = null;

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            Quaternion[] rotation = null;

            // initial...
            if(initialState || lastDeserializedRotations == null)
                lastDeserializedRotations = new Quaternion[PoseSnapshot.MAX_SIZE];

            // ... and delta
            rotation = reader.ReadPoseSnapshot(ref lastDeserializedRotations, compressRotation);

            // handle depending on server / client / host.
            // server has priority for host mode.
            if      (isServer) OnClientToServerSync(rotation);
            else if (isClient) OnServerToClientSync(rotation);

        }

        // update //////////////////////////////////////////////////////////////
        void UpdateServer()
        {
            // set dirty to trigger OnSerialize. either always, or only if changed.
            // technically snapshot interpolation requires constant sending.
            // however, with reliable it should be fine without constant sends.
            if (!onlySyncOnChange || Changed(Construct()))
                SetDirty();

            // apply buffered snapshots IF client authority
            // -> in server authority, server moves the object
            //    so no need to apply any snapshots there.
            // -> don't apply for host mode player objects either, even if in
            //    client authority mode. if it doesn't go over the network,
            //    then we don't need to do anything.
            // -> connectionToClient is briefly null after scene changes:
            //    https://github.com/MirrorNetworking/Mirror/issues/3329
            if (syncDirection == SyncDirection.ClientToServer &&
                connectionToClient != null &&
                !isOwned)
            {
                if (serverSnapshots.Count > 0)
                {
                    // step the transform interpolation without touching time.
                    // NetworkClient is responsible for time globally.
                    SnapshotInterpolation.StepInterpolation(
                        serverSnapshots,
                        connectionToClient.remoteTimeline,
                        out PoseSnapshot from,
                        out PoseSnapshot to,
                        out double t);

                    // interpolate & apply
                    PoseSnapshot computed = PoseSnapshot.Interpolate(from, to, t);
                    Apply(computed);
                }
            }
        }

        int lastClientCount = 0;
        void UpdateClient()
        {
            // client authority, and local player (= allowed to move myself)?
            if (IsClientWithAuthority)
            {
                // https://github.com/vis2k/Mirror/pull/2992/
                if (!NetworkClient.ready) return;

                // set dirty to trigger OnSerialize. either always, or only if changed.
                // technically snapshot interpolation requires constant sending.
                // however, with reliable it should be fine without constant sends.
                if (!onlySyncOnChange || Changed(Construct()))
                    SetDirty();
            }
            // for all other clients (and for local player if !authority),
            // we need to apply snapshots from the buffer
            else
            {

                // only while we have snapshots
                if (clientSnapshots.Count > 0)
                {

                    // step the interpolation without touching time.
                    // NetworkClient is responsible for time globally.
                    SnapshotInterpolation.StepInterpolation(
                        clientSnapshots,
                        NetworkTime.time, // == NetworkClient.localTimeline from snapshot interpolation
                        out PoseSnapshot from,
                        out PoseSnapshot to,
                        out double t);

                    // interpolate & apply
                    PoseSnapshot computed = PoseSnapshot.Interpolate(from, to, t);
                    Apply(computed);

                }

                // 'only sync if moved'
                // explain..
                // from 1 snap to next snap..
                // it'll be old...
                if (lastClientCount > 1 && clientSnapshots.Count == 1)
                {
                    // this is it. snapshots are down to '1'.
                    // does this cause stuck?
                }

                lastClientCount = clientSnapshots.Count;
            }
        }

        void Update()
        {
            // if server then always sync to others.
            if      (isServer) UpdateServer();
            // 'else if' because host mode shouldn't send anything to server.
            // it is the server. don't overwrite anything there.
            else if (isClient) UpdateClient();
        }

        public override void Reset()
        {
            base.Reset();
        }
    }
}
