using System;

namespace Arteranos.Audio
{
    public interface IVoiceInput
    {
        int ChannelCount { get; }
        int SampleRate { get; }
        float Gain { get; set; }

        event Action<float[]> OnSampleReady;
        event Action<int, byte[]> OnSegmentReady;

        void SetAGCLevel(int level);
    }
}