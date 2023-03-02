using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Arteranos.UniVoice;
using POpusCodec;
using POpusCodec.Enums;
using Arteranos.Core;

namespace Arteranos.Audio
{
    public class UVAudioOutput : MonoBehaviour, IAudioOutputV2, IVoiceOutput
    {
        public AudioSource AudioSource { get; private set; }

        private OpusDecoder decoder;
        private RingBuffer<float[]> frameBuffer = null;
        private RingBuffer<float> vuBuffer = null;
        private int SamplingRate;
        private int ChannelCount;

        private int FrameSize = -1;
        public string ID
        {
            get => gameObject.name;
            set => gameObject.name = $"UniVoice Peer #{value}";
        }

        [System.Obsolete("Cannot use new keyword to create an instance. Use .New() method instead")]
        public UVAudioOutput() { }

        public static UVAudioOutput New(int samplingRate, int channelCount)
        {
            GameObject go = new($"UniVoiceAudioSourceOutput");
            DontDestroyOnLoad(go);
            go.transform.SetParent(Core.SettingsManager.Purgatory);
            go.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            UVAudioOutput ctd = go.AddComponent<UVAudioOutput>();

            return ctd.SetupDecoder(go.AddComponent<AudioSource>(), samplingRate, channelCount);
        }

        private UVAudioOutput SetupDecoder(AudioSource source, int samplingRate, int channelCount)
        {
            SamplingRate = samplingRate;
            ChannelCount = channelCount;

            AudioSource = source;
            decoder = new OpusDecoder((SamplingRate) SamplingRate, (Channels) ChannelCount);
            vuBuffer = new(samplingRate);
            vuBuffer.PushBack(0.0f);

            AudioClip myClip = AudioClip.Create("MyPlayback",
                SamplingRate,
                ChannelCount,
                SamplingRate,
                false);
            AudioSource.loop = true;
            AudioSource.clip = myClip;
            AudioSource.spatialBlend = 1.0f;

            return this;
        }

        // NB: A streaming AudioSource buffers at around one second, regardless the
        //     sampling rate. To synchronize, we have to use a sliding window.
        public void Feed(byte[] encodedData)
        {
            // FIXME Concurrency?
            // Tack on the decoded data to the receive buffer.
            float[] samples = decoder.DecodePacketFloat(encodedData);
            
            if(FrameSize < 0)
            {
                FrameSize = samples.Length;
                frameBuffer = new(SamplingRate / FrameSize);

                Debug.Log($"FrameSize={FrameSize}, {frameBuffer.Capacity} frames/s");
            }

            foreach(float sample in samples)
                AdvanceCharge(sample);

            frameBuffer.PushBack(samples);
        }

        private float charge = 0;
        private const float kCharge = 0.1f;
        private const float kDischarge = 0.05f;

        private void AdvanceCharge(float value)
        {
            value = Mathf.Abs(value);

            if(value > charge)
                charge = (charge * (1 - kCharge)) + (value * kCharge);
            else
                charge *= (1 - kDischarge);

            vuBuffer.PushBack(value);
        }

        private int usingFrame = 0;
        private void Update()
        {
            if(frameBuffer == null) return;

            if(frameBuffer.Size < 3)
            {
                AudioSource.Stop();
                usingFrame = 0;
                return;
            }
            else if(frameBuffer.Size > 5 && !AudioSource.isPlaying)
            {
                AudioSource.Play();
            }

            while (frameBuffer.Size > 3)
            {
                AudioSource.clip.SetData(frameBuffer.Front(), (usingFrame++ % frameBuffer.Capacity) * FrameSize);
                frameBuffer.PopFront();
            }
        }

        /// <summary>
        /// Disposes the instance by deleting the GameObject of the component.
        /// </summary>
        public void Dispose()
        {
            AudioSource.Stop();
            decoder.Dispose();
            Destroy(gameObject);
        }


        public float MeasureAmplitude() => vuBuffer.Front();

        /// <summary>
        /// Creates <see cref="UVAudioOutput"/> instances
        /// </summary>
        public class Factory : IAudioOutputFactoryV2
        {
            public Factory() { }

            public IAudioOutputV2 Create(int samplingRate, int channelCount) => New(samplingRate, channelCount);
        }
    }
}
