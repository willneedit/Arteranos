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

        private AudioClip recorderClip { 
            get => audiorecorder.clip; 
            set => audiorecorder.clip = value;
        }

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

        public static UVMicInput New(int? micDeviceId = 0, int? desiredRate = null)
        {
            GameObject go = new("_Microphone");
            DontDestroyOnLoad(go);
            go.transform.SetParent(Core.SettingsManager.Purgatory);
            go.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            return go.AddComponent<UVMicInput>().New_(micDeviceId, desiredRate);
        }

        public static UVMicInput Renew(int? micDeviceId = 0, int? desiredRate = null)
        {
            UVMicInput uvmi = FindObjectOfType<UVMicInput>();
            return uvmi.Renew_(micDeviceId, desiredRate);
        }

        private UVMicInput Renew_(int? micDeviceId, int? desiredRate)
        {
            SetupMic(micDeviceId, desiredRate, true);

            return this;
        }

        private UVMicInput New_(int? micDeviceId, int? desiredRate)
        {
            audiorecorder = gameObject.AddComponent<AudioSource>();
            audiorecorder.loop = true;

            SetupMic(micDeviceId, desiredRate);

            encoder = new((SamplingRate) SampleRate, (Channels) ChannelCount)
            {
                EncoderDelay = Delay.Delay20ms,
                SignalHint = SignalHint.Voice,
                MaxBandwidth = Bandwidth.Wideband,
                Bitrate = SampleRate
            };

            //encoder.ForceChannels = POpusCodec.Enums.ForceChannels.NoForce;
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

        private void SetupMic(int? micDeviceId, int? desiredRate, bool renew = false)
        {
            string oldDeviceName = deviceName;

            SampleRate = desiredRate ?? AudioSettings.outputSampleRate;
            SampleRate = ValidateSampleRate(SampleRate);

            deviceName = micDeviceId.HasValue ? Microphone.devices[micDeviceId.Value] : null;

            Debug.Log($"setup mic with {deviceName}, samplerate={SampleRate}");

            if(renew)
                Microphone.End(oldDeviceName);

            recorderClip = Microphone.Start(
                deviceName,
                true,
                1,
                SampleRate);

            ChannelCount = recorderClip.channels;

            Debug.Log($"Clip samples={recorderClip.samples}, channels={ChannelCount}");
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


            // was SampleRate / 10 -- the amount of 1/10th of a second,
            // and with the 20ms encoder delay, five packets amount to 100ms.
            float[] temp = new float[packetSize * 5];

            while(true)
            {
                AudioClip AudioClip = recorderClip;

                while(AudioClip != null)
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
