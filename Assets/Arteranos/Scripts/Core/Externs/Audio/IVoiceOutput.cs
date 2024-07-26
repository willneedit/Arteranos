using UnityEngine;

namespace Arteranos.Audio
{
    public interface IVoiceOutput
    {
        bool mute { get; set; }

        public GameObject gameObject { get; }

        public Transform transform { get; }

        /// <summary>
        /// Measures the amplitude of the upcoming audio data
        /// </summary>
        /// <returns>Height of amplitude, ranged [0...1]</returns>
        float MeasureAmplitude();

        /// <summary>
        /// Feeds the data to the output implementation 
        /// </summary>
        /// <param name="audioSamples">
        /// The audio samples/segment being fed
        /// </param>
        void Feed(byte[] audioSamples);
    }
}