using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Adrenak.UniVoice;
using Arteranos.ExtensionMethods;

namespace Arteranos.Audio
{
    /// <summary>
    /// This class feeds incoming segments of audio to an AudioBuffer 
    /// and plays the buffer's clip on an AudioSource. It also clears segments
    /// of the buffer based on the AudioSource's position.
    /// </summary>
    public class UVAudioOutput : MonoBehaviour, IAudioOutput
    {
        enum Status
        {
            Ahead,
            Current,
            Behind
        }

        private readonly Dictionary<int, Status> segments = new();
        int GetSegmentCountByStatus(Status status)
        {
            IEnumerable<KeyValuePair<int, Status>> matches = segments.Where(x => x.Value == status);
            if(matches == null) return 0;
            return matches.Count();
        }

        public AudioSource AudioSource { get; private set; }
        public int MinSegCount { get; private set; }

        RingBuffer RingBuffer;

        public string ID
        {
            get => RingBuffer.AudioClip.name;
            set
            {
                gameObject.name = "UniVoice Peer #" + value;
                RingBuffer.AudioClip.name = "UniVoice Peer #" + value;
            }
        }

        [System.Obsolete("Cannot use new keyword to create an instance. Use .New() method instead")]
        public UVAudioOutput() { }

        /// <summary>
        /// Creates a new instance using the dependencies.
        /// </summary>
        /// 
        /// <param name="buffer">
        /// The AudioBuffer that the streamer operates on.
        /// </param>
        /// 
        /// <param name="source">
        /// The AudioSource from where the incoming audio is played.
        /// </param>
        /// 
        /// <param name="minSegCount">
        /// The minimum number of audio segments <see cref="RingBuffer"/> 
        /// must have for the streamer to play the audio. This value is capped
        /// between 1 and <see cref="Audio.RingBuffer.SegCount"/> of the 
        /// <see cref="RingBuffer"/> passed.
        /// Default: 0. Results in the value being set to the max possible.
        /// </param>
        public static UVAudioOutput New(RingBuffer buffer, AudioSource source, int minSegCount = 0)
        {
            GameObject go = source.gameObject;
            DontDestroyOnLoad(go);
            go.transform.SetParent(Core.SettingsManager.Purgatory);

            UVAudioOutput ctd = go.AddComponent<UVAudioOutput>();

            source.loop = true;
            source.clip = buffer.AudioClip;

            if(minSegCount != 0)
                ctd.MinSegCount = Mathf.Clamp(minSegCount, 1, buffer.SegCount);
            else
                ctd.MinSegCount = buffer.SegCount;
            ctd.RingBuffer = buffer;
            ctd.AudioSource = source;

            return ctd;
        }

        int lastIndex = -1;
        /// <summary>
        /// This is to make sure that if a segment is missed, its previous 
        /// contents won't be played again when the clip loops back.
        /// </summary>
        private void Update()
        {
            if(AudioSource.clip == null) return;

            int index = (int) (AudioSource.GetCurrentPosition() * RingBuffer.SegCount);

            // Check every frame to see if the AudioSource has 
            // just moved to a new segment in the AudioBuffer 
            if(lastIndex != index)
            {
                // If so, clear the audio buffer so that in case the
                // AudioSource loops around, the old contents are not played.
                RingBuffer.Clear(lastIndex);

                segments.EnsureKey(lastIndex, Status.Behind);
                segments.EnsureKey(index, Status.Current);

                lastIndex = index;
            }

            // Check if the number of ready segments is sufficient for us to 
            // play the audio. Whereas if the number is 0, we must stop audio
            // and wait for the minimum ready segment count to be met again.
            int readyCount = GetSegmentCountByStatus(Status.Ahead);
            if(readyCount == 0)
            {
                AudioSource.mute = true;
            }
            else if(readyCount >= MinSegCount)
            {
                AudioSource.mute = false;
                if(!AudioSource.isPlaying)
                    AudioSource.Play();
            }
        }

        /// <summary>
        /// Feeds incoming audio into the audio buffer.
        /// </summary>
        /// 
        /// <param name="index">
        /// The absolute index of the segment, as reported by the peer to know 
        /// the normalized position of the segment on the buffer
        /// </param>
        /// 
        /// <param name="audioSamples">The audio samples being fed</param>
        public void Feed(int index, int frequency, int channelCount, float[] audioSamples)
        {
            // If we already have this index, don't bother
            // It's been passed already without playing.
            if(segments.ContainsKey(index)) return;

            int locIdx = (int) (AudioSource.GetCurrentPosition() * RingBuffer.SegCount);
            locIdx = Mathf.Clamp(locIdx, 0, RingBuffer.SegCount - 1);

            int bufferIndex = RingBuffer.GetNormalizedIndex(index);

            // Don't write to the same segment index that we are reading
            if(locIdx == bufferIndex) return;

            // Finally write into the buffer 
            segments.Add(index, Status.Ahead);
            RingBuffer.Write(index, audioSamples);
        }

        /// <summary>
        /// Disposes the instance by deleting the GameObject of the component.
        /// </summary>
        public void Dispose() => Destroy(gameObject);

        /// <summary>
        /// Creates <see cref="UVAudioOutput"/> instances
        /// </summary>
        public class Factory : IAudioOutputFactory
        {
            public int BufferSegCount { get; private set; }
            public int MinSegCount { get; private set; }

            public Factory() : this(10, 5) { }

            public Factory(int bufferSegCount, int minSegCount)
            {
                BufferSegCount = bufferSegCount;
                MinSegCount = minSegCount;
            }

            public IAudioOutput Create(int samplingRate, int channelCount, int segmentLength)
            {
                return New(
                    new RingBuffer(samplingRate, channelCount, segmentLength, BufferSegCount),
                    new GameObject($"UniVoiceAudioSourceOutput").AddComponent<AudioSource>(),
                    MinSegCount
                );
            }
        }
    }
}
