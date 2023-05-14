using UnityEngine;

using Arteranos.UniVoice;
using POpusCodec;
using POpusCodec.Enums;
using Arteranos.Core;
using UnityEngine.Audio;

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
        private float charge = 0;

        public string ID
        {
            get => gameObject.name;
            set => gameObject.name = $"UniVoice Peer #{value}";
        }

        [System.Obsolete("Cannot use new keyword to create an instance. Use .New() method instead")]
        public UVAudioOutput() { }

        public static UVAudioOutput New(int samplingRate, int channelCount, AudioMixerGroup mg)
        {
            GameObject go = new($"UniVoiceAudioSourceOutput");
            DontDestroyOnLoad(go);
            go.transform.SetParent(Core.SettingsManager.Purgatory);
            go.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            UVAudioOutput ctd = go.AddComponent<UVAudioOutput>();
            AudioSource source = go.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = mg;

            return ctd.SetupDecoder(source, samplingRate, channelCount);
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
            {
                Utils.CalcVU(sample, ref charge, 0.1f, 0.02f);
                vuBuffer.PushBack(charge);
            }

            frameBuffer.PushBack(samples);
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
            private readonly AudioMixerGroup mg;
            public Factory(AudioMixerGroup _mg) { mg = _mg; }

            public IAudioOutputV2 Create(int samplingRate, int channelCount) => New(samplingRate, channelCount, mg);
        }
    }
}
