using System;

namespace Arteranos.UniVoice {
    /// <summary>
    /// Responsible for playing audio that is sent to it. 
    /// You'd normally want a <see cref="UnityEngine.AudioSource"/> 
    /// based implementation to play the audio in Unity. But this class can 
    /// be used in other ways just as streaming the received audio to a server
    /// or writing it to a local file. It's just an audio output and the 
    /// destination doesn't matter.
    /// </summary>
    public interface IAudioOutputV2 : IDisposable {
        /// <summary>
        /// An ID associated with this audio output
        /// </summary>
        string ID { get; set; }

        /// <summary>
        /// Feeds the data to the output implementation 
        /// </summary>
        /// <param name="audioSamples">
        /// The audio samples/segment being fed
        /// </param>
        void Feed(byte[] audioSamples);
    }
}
