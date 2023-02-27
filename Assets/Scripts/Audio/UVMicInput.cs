using System;
using System.Collections;
using System.Collections.Generic;
using Arteranos.UniVoice;

using POpusCodec;
using POpusCodec.Enums;
using UnityEngine;

namespace Arteranos.Audio
{
    /// <summary>
    /// An <see cref="IAudioInput"/> implementation based on UniMic.
    /// For more on UniMic, visit https://www.github.com/adrenak/unimic
    /// </summary>
    public class UVMicInput : MonoBehaviour, IAudioInputV2
    {
        public event Action<int, byte[]> OnSegmentReady;

        public int SampleRate { get; private set; }
        public int ChannelCount { get; private set; }

        public string deviceName;

        private AudioSource audiorecorder = null;
        private readonly List<float> micBuffer = new();
        private OpusEncoder encoder;
        private int packetndex = 0;
        private int packetSize;

        private event Action<float[]> OnSampleReady;

        [Obsolete("Cannot use new keyword to create an instance. Use .New() method instead")]
        public UVMicInput() { }

        private int ValidateSampleRate(int sampleRate)
        {
            switch((SamplingRate) sampleRate)
            {
                case SamplingRate.Sampling08000:
                    break;
                case SamplingRate.Sampling12000:
                    break;
                case SamplingRate.Sampling16000:
                    break;
                case SamplingRate.Sampling24000:
                    break;
                case SamplingRate.Sampling48000:
                    break;
                default:
                    Debug.LogWarning($"SamplingRate of {sampleRate} outside of the Opus specification, setting to 8kHz");
                    sampleRate = (int) SamplingRate.Sampling08000;
                    break;
            }

            return sampleRate;
        }

        public static UVMicInput New(int micDeviceId = 0, int? desiredRate = null)
        {
            GameObject go = new("_Microphone");
            DontDestroyOnLoad(go);
            go.transform.SetParent(Core.SettingsManager.Purgatory);
            go.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            return go.AddComponent<UVMicInput>().New_(micDeviceId, desiredRate);
        }

        private UVMicInput New_(int micDeviceId, int? desiredRate)
        {
            SampleRate = desiredRate ?? AudioSettings.outputSampleRate;
            SampleRate = ValidateSampleRate(SampleRate);

            deviceName = Microphone.devices[micDeviceId];

            Debug.Log($"setup mic with {deviceName}, samplerate={SampleRate}");
            audiorecorder = gameObject.AddComponent<AudioSource>();
            audiorecorder.loop = true;
            audiorecorder.clip = Microphone.Start(
                deviceName,
                true,
                1,
                SampleRate);

            ChannelCount = audiorecorder.clip.channels;

            Debug.Log($"Clip samples={audiorecorder.clip.samples}, channels={ChannelCount}");

            // The SamplingRate enum match the actual numbers, as well as the Mono/Stereo,
            // let's hope....
            encoder = new((SamplingRate) SampleRate, (Channels) ChannelCount)
            {
                EncoderDelay = Delay.Delay20ms,
                SignalHint = SignalHint.Voice,
                MaxBandwidth = Bandwidth.Wideband
            };

            //encoder.ForceChannels = POpusCodec.Enums.ForceChannels.NoForce;
            //encoder.Bitrate = samplerate;
            //encoder.Complexity = POpusCodec.Enums.Complexity.Complexity0;
            //encoder.DtxEnabled = true;
            //encoder.ExpectedPacketLossPercentage = 0;
            //encoder.UseInbandFEC = true;
            //encoder.UseUnconstrainedVBR = true;

            Debug.Log($"Framesize: {encoder.FrameSizePerChannel}, {encoder.InputChannels}");

            // the encoder delay has some influence on the amout of data we need to send, but it's not a multiplication of it
            packetSize = encoder.FrameSizePerChannel * ChannelCount;

            OnSampleReady += DeliverCompressedAudio;
            StartCoroutine(ReadRawAudio());

            return this;
        }

        private void OnDestroy()
        {
            StopCoroutine(ReadRawAudio());
            OnSampleReady -= DeliverCompressedAudio;
            encoder.Dispose();
        }

        IEnumerator ReadRawAudio()
        {
            int loops = 0;
            int readAbsPos = 0;
            int prevPos = 0;

            AudioClip AudioClip = audiorecorder.clip;

            int ClipDataLength = SampleRate / 10;

            float[] temp = new float[ClipDataLength];

            while(true)
            {
                while(true)
                {
                    int currPos = Microphone.GetPosition(deviceName);
                    if(currPos < prevPos)
                        loops++;
                    prevPos = currPos;

                    int currAbsPos = loops * AudioClip.samples + currPos;
                    int nextReadAbsPos = readAbsPos + temp.Length;

                    if(nextReadAbsPos >= currAbsPos) break;

                    AudioClip.GetData(temp, readAbsPos % AudioClip.samples);

                    OnSampleReady?.Invoke(temp);

                    readAbsPos = nextReadAbsPos;
                }
                yield return null;
            }
        }

        private void DeliverCompressedAudio(float[] data)
        {
            micBuffer.AddRange(data);

            int packets = micBuffer.Count / packetSize;
            for(int i = 0; i < packets; i++)
            {
                byte[] encodedData = encoder.Encode(micBuffer.GetRange(i * packetSize, packetSize).ToArray());
                OnSegmentReady?.Invoke(packetndex++, encodedData);
            }
            micBuffer.RemoveRange(0, packets * packetSize);
        }

        public void Dispose() => Destroy(audiorecorder.gameObject);
    }
}
