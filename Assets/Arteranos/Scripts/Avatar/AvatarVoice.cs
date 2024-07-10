/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Services;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Arteranos.Audio;

namespace Arteranos.Avatar
{
    internal struct VoiceSegment
    {
        public int sampleRate;
        public int channelCount;
        public int index;
        public byte[] samples;
    }

    internal struct VoicePacket
    {
        public uint senderNetID;
        public uint[] receiverNetID;
        public VoiceSegment data;
    }

    public class AvatarVoice : NetworkBehaviour
    {
        private IAvatarBrain Brain => gameObject.GetComponent<AvatarBrain>();
        private bool IsMuted => AppearanceStatus.IsSilent(Brain.AppearanceStatus);
        // private bool IsMutedTo(uint netID) => IsMutedTo(G.NetworkStatus.GetOnlineUser(netID));
        private bool IsMutedTo(IAvatarBrain other) => 
            (other != null) && AppearanceStatus.IsSilent(other.AppearanceStatus);
        private IVoiceOutput AudioOutput { get; set; } = null;



        public override void OnStartClient()
        {
            base.OnStartClient();

            if(isLocalPlayer)
                G.AudioManager.OnSegmentReady += ReceivedMicInput;
        }

        public override void OnStopClient()
        {
            if(isLocalPlayer)
                G.AudioManager.OnSegmentReady -= ReceivedMicInput;

            base.OnStopClient();
        }

        private void ReceivedMicInput(int index, byte[] samples)
        {
            // No point to emit the audio data if the user is self-muted.
            if(IsMuted) return;

            VoicePacket packet = new()
            {
                senderNetID = Brain.NetID,
                receiverNetID = null, // Everyone available
                data = new VoiceSegment()
                {
                    channelCount = G.AudioManager.ChannelCount,
                    sampleRate = G.AudioManager.SampleRate,
                    index = index,
                    samples = samples
                }
            };

            CmdReceivedMicInput(packet);
        }

        private void ReceivedNetworkInput(VoicePacket voicePacket, bool muted)
        {
            if(AudioOutput == null)
            {
                // FIXME what with channel count and sample rate mismatch?
                AudioOutput = G.AudioManager.GetVoiceOutput(
                    voicePacket.data.sampleRate, voicePacket.data.channelCount);

                IAvatarBrain _ = G.NetworkStatus.GetOnlineUser(voicePacket.senderNetID);

                AudioOutput.transform.SetParent(transform, false);
            }

            AudioOutput.mute = muted;

            if(muted) return;

            AudioOutput.Feed(voicePacket.data.samples);
        }

        public float MeasureAmplitude() => AudioOutput?.MeasureAmplitude() ?? 0;

        [Command]
        private void CmdReceivedMicInput(VoicePacket voicePacket)
        {
            IEnumerable<IAvatarBrain> q = from user in G.NetworkStatus.GetOnlineUsers()
                    where (user.NetID != voicePacket.senderNetID) 
                       && (voicePacket.receiverNetID == null 
                        || voicePacket.receiverNetID.Contains(user.NetID))
                    select user;
            
            // Send to whom is concerned
            // TODO mute-others culling - needs mappings
            foreach(IAvatarBrain user in q) 
            {
                // No sense if the speaker is too far away, skip it.
                // FIXME hardcoded 40 meters
                if(Vector3.SqrMagnitude(transform.position
                    - user.transform.position)
                    > 1600.0f)
                {
                    continue;
                }

                user.gameObject.GetComponent<AvatarVoice>()
                               .TargetReceivedVoicePacket(voicePacket);
            }
        }

        [TargetRpc]
        private void TargetReceivedVoicePacket(VoicePacket voicePacket)
        {
            IAvatarBrain emitter = G.NetworkStatus.GetOnlineUser(voicePacket.senderNetID);

            if(emitter == null)
            {
                Debug.Log($"Received a voice packet from unknown client with NetID {voicePacket.senderNetID}");
                return;
            }

            // While the sound is heard by you, it has to be located at the sound source.
            emitter.gameObject.GetComponent<AvatarVoice>()
                              .ReceivedNetworkInput(voicePacket, IsMutedTo(emitter));
        }
    }
}
