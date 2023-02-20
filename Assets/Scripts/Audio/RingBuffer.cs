using UnityEngine;

namespace Arteranos.Audio
{
    /// <summary>
    /// Used to arrange irregular, out of order 
    /// and skipped audio segments for better playback.
    /// </summary>
    public class RingBuffer
    {
        public AudioClip AudioClip { get; private set; }
        public int SegCount { get; private set; }
        public int SegDataLen { get; private set; }

        private readonly float[] emptyBuffer = null;

        // Holds the first valid segment index received by the buffer
        private int indexOffset = -1;

        /// <summary>
        /// Create an instance
        /// </summary>
        /// <param name="sampleRate">The sample rate of the audio</param>
        /// <param name="channels">Number of channels in the audio</param>
        /// <param name="segDataLen">Number of samples in the audio</param>
        /// <param name="segCount">Number of segments stored in buffer</param>
        public RingBuffer(int sampleRate, int channels, int segDataLen, int segCount = 3, string clipName = null)
        {
            clipName ??= "clip";
            AudioClip = AudioClip.Create(clipName, segDataLen * segCount, channels, sampleRate, false);

            SegDataLen = segDataLen;
            SegCount = segCount;

            // Set aside an empty buffer to avoid repeated allocations
            emptyBuffer = new float[segDataLen];
        }

        /// <summary>
        /// Feed an audio segment to the buffer.
        /// </summary>
        /// 
        /// <param name="absoluteIndex">
        /// Absolute index of the audio segment from the source.
        /// </param>
        /// 
        /// <param name="audioSegment">Audio samples data</param>
        public bool Write(int absoluteIndex, float[] audioSegment)
        {
            // Reject if the segment length is wrong
            if(audioSegment.Length != SegDataLen) return false;

            // If it's the very first segment to play, adjust the offset
            if(indexOffset == -1) indexOffset = absoluteIndex;

            // Convert the absolute index into a looped-around index
            int localIndex = GetNormalizedIndex(absoluteIndex);

            // Set the segment at the clip data at the right index
            if(localIndex >= 0)
                AudioClip.SetData(audioSegment, localIndex * SegDataLen);
            return true;
        }

        /// <summary>
        /// Returns the index after looping around the buffer
        /// </summary>
        public int GetNormalizedIndex(int absoluteIndex)
        {
            int localIndex = absoluteIndex - indexOffset;
            if(indexOffset < 0|| localIndex < 0) return -1;
            return localIndex % SegCount;
        }

        /// <summary>
        /// Clears the buffer at the specified local index
        /// </summary>
        /// <param name="index"></param>
        public bool Clear(int index)
        {
            if(index < 0) return false;

            // If the index is out of bounds, then we
            // loop that around and use the local index
            if(index >= SegCount)
                index = GetNormalizedIndex(index);
            AudioClip.SetData(emptyBuffer, index * SegDataLen);
            return true;
        }

        /// <summary>
        /// Clear the entire buffer
        /// </summary>
        public bool Clear() => AudioClip.SetData(new float[SegDataLen * SegCount], 0);
    }
}