using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Adrenak.UniVoice;
using POpusCodec;
using POpusCodec.Enums;

namespace Arteranos.Audio
{
    public class UVAudioOutput : MonoBehaviour, IAudioOutputV2
    {
        public AudioSource AudioSource { get; private set; }
        public OpusDecoder decoder;

        public readonly List<float> receiveBuffer = new();

        public Channels opusChannels = Channels.Mono;
        public SamplingRate opusSamplingRate = SamplingRate.Sampling48000;

        public string ID
        {
            get => gameObject.name;
            set => gameObject.name = $"UniVoice Peer #{value}";
        }

        [System.Obsolete("Cannot use new keyword to create an instance. Use .New() method instead")]
        public UVAudioOutput() { }

        public static UVAudioOutput New(int samplingRate, int channelCount)
        {
            Debug.Assert(samplingRate == 48000);
            Debug.Assert(channelCount == 1);

            GameObject go = new($"UniVoiceAudioSourceOutput");
            DontDestroyOnLoad(go);
            go.transform.SetParent(Core.SettingsManager.Purgatory);

            UVAudioOutput ctd = go.AddComponent<UVAudioOutput>();

            return ctd.SetupDecoder(go.AddComponent<AudioSource>());
        }

        private UVAudioOutput SetupDecoder(AudioSource source) 
        {
            AudioSource = source;
            decoder = new OpusDecoder(opusSamplingRate, opusChannels);

            AudioClip myClip = AudioClip.Create("MyPlayback", (int) opusSamplingRate, (int) opusChannels, (int) opusSamplingRate, true, OnPlaybackRead, OnPlaybackSetPosition);
            AudioSource.loop = true;
            AudioSource.clip = myClip;
            AudioSource.Play();

            return this;
        }

        public void Feed(int _, int frequency, int channelCount, byte[] encodedData)
        {
            Debug.Assert(frequency == 48000);
            Debug.Assert(channelCount == 1);

            // Tack on the decoded data to the receive buffer.
            receiveBuffer.AddRange(decoder.DecodePacketFloat(encodedData));
        }

        void OnPlaybackRead(float[] data)
        {
            int pullSize = Mathf.Min(data.Length, receiveBuffer.Count);
            float[] dataBuf = receiveBuffer.GetRange(0, pullSize).ToArray();
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
        public void Dispose() => Destroy(gameObject);

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
