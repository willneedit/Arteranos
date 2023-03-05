namespace Arteranos.UniVoice {
    /// <summary>
    /// An abstract factory that creates <see cref="IAudioOutput"/> based on
    /// given parameters.
    /// </summary>
    public interface IAudioOutputFactoryV2 {
        /// <summary>
        /// Creates an instance of a concrete <see cref="IAudioOutput"/> class
        /// </summary>
        /// <param name="peerID">The ID of the peer for which </param>
        /// <param name="frequency">Frequency/sample rate of the audio </param>
        /// <param name="channelCount">Number of audio channels in data</param>
        /// <param name="samplesLen">Number of samples in audio segment</param>
        IAudioOutputV2 Create(int frequency, int channelCount);
    }
}