namespace Adrenak.UniVoice {

    /// <summary>
    /// A data structure representing the audio transmitted over the network.
    /// </summary>
    public struct ChatroomAudioSegmentV2 {
        /// <summary>
        /// A float array representing the audio sample data
        /// </summary>
        public byte[] samples;
    }
}