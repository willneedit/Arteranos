using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Arteranos.UniVoice;
using POpusCodec;
using POpusCodec.Enums;

namespace Arteranos.Audio
{
    public class UVAudioOutput : MonoBehaviour, IAudioOutputV2, IVoiceOutput
    {
        public AudioSource AudioSource { get; private set; }

        private OpusDecoder decoder;
        private readonly List<float> receiveBuffer = new();
        private int SamplingRate;
        private int ChannelCount;

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

            AudioClip myClip = AudioClip.Create("MyPlayback",
                SamplingRate,
                ChannelCount,
                SamplingRate,
                true, OnPlaybackRead, OnPlaybackSetPosition);
            AudioSource.loop = true;
            AudioSource.clip = myClip;
            AudioSource.spatialBlend = 1.0f;
            AudioSource.Play();

            return this;
        }

        public void Feed(byte[] encodedData)
        {
            // FIXME Concurrency?
            // Tack on the decoded data to the receive buffer.
            float[] samples = decoder.DecodePacketFloat(encodedData);
            receiveBuffer.AddRange(samples);
        }

        private volatile float charge = 0;
        private const float kCharge = 0.1f;
        private const float kDischarge = 0.05f;

        private void AdvanceCharge(float value)
        {
            value = Mathf.Abs(value);

            if(value > charge)
                charge = (charge * (1 - kCharge)) + (value * kCharge);
            else
                charge *= (1 - kDischarge);
        }

        void OnPlaybackRead(float[] data)
        {
            int pullSize = Mathf.Min(data.Length, receiveBuffer.Count);
            float[] dataBuf = receiveBuffer.GetRange(0, pullSize).ToArray();

            foreach(float sample in dataBuf)
                AdvanceCharge(sample);

            dataBuf.CopyTo(data, 0);
            receiveBuffer.RemoveRange(0, pullSize);

            // clear rest of data
            for(int i = pullSize; i < data.Length; i++)
                data[i] = 0;
        }

        void OnPlaybackSetPosition(int newPosition)
        {
            // we dont need the audio position at the moment
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


        public float MeasureAmplitude() => charge;

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
