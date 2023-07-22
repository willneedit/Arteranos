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
        private bool IsSelfMuted => AppearanceStatus.IsSilent(Brain.AppearanceStatus);
        private bool IsMuted(uint netID) => IsMuted(NetworkStatus.GetOnlineUser(netID));
        private bool IsMuted(IAvatarBrain other) => 
            (other != null) && AppearanceStatus.IsSilent(other.AppearanceStatus);
        private IVoiceOutput AudioOutput { get; set; } = null;



        // TODO Migrate use of UVMicInput from AudioManager
        // TODO migrate use of UVAudioOutput from AudioManager
        // or, fire ChatroomAgent :)
        public override void OnStartClient()
        {
            base.OnStartClient();

            if(isOwned)
            {
                AudioManager.OnSegmentReady += ReceivedMicInput;
            }
            else
            {

            }
        }

        public override void OnStopClient()
        {
            if(isOwned)
            {
                AudioManager.OnSegmentReady -= ReceivedMicInput;
            }
            else
            {

            }

            base.OnStopClient();
        }

        private void ReceivedMicInput(int index, byte[] samples)
        {
            // No point to emit the audio data if the user is self-muted.
            if(IsSelfMuted) return;

            VoicePacket packet = new()
            {
                senderNetID = Brain.NetID,
                receiverNetID = null, // Everyone available
                data = new VoiceSegment()
                {
                    channelCount = AudioManager.Instance.ChannelCount,
                    sampleRate = AudioManager.Instance.SampleRate,
                    index = index,
                    samples = samples
                }
            };

            CmdReceivedMicInput(packet);
        }

        private void ReceivedNetworkInput(VoicePacket voicePacket)
        {
            if(AudioOutput == null)
            {
                // FIXME what with channel count and sample rate mismatch?
                AudioOutput = AudioManager.Instance.GetVoiceOutput(
                    voicePacket.data.sampleRate, voicePacket.data.channelCount);

                AudioOutput.transform.SetParent(transform, false);
            }

            AudioOutput.Feed(voicePacket.data.samples);
        }

        [Command]
        private void CmdReceivedMicInput(VoicePacket voicePacket)
        {
            IEnumerable<IAvatarBrain> q = from user in NetworkStatus.GetOnlineUsers()
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
                    - user.gameObject.transform.position)
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
            // Sender is muted by you
            if(IsMuted(voicePacket.senderNetID)) return;

            ReceivedNetworkInput(voicePacket);
        }
    }
}
