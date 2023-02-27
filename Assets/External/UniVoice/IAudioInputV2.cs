using System;

namespace Arteranos.UniVoice {
    /// <summary>
    /// Source of user voice input. This would usually be implemented 
    /// over a microphone to get the users voice. But it can also be used
    /// in other ways such as streaming an mp4 file from disk. It's just 
    /// an input and the source doesn't matter.
    /// </summary>
    public interface IAudioInputV2 : IDisposable {
        /// <summary>
        /// Fired when a segment (sequence of audio samples) is ready
        /// </summary>
        event Action<int, byte[]> OnSegmentReady;

        /// <summary>
        /// The sampling frequency of the audio
        /// </summary>
        int SampleRate { get; }

        /// <summary>
        /// The number of channels in the audio
        /// </summary>
        int ChannelCount { get; }

    }
}
