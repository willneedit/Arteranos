using System;

using Adrenak.UniMic;
using Adrenak.UniVoice;

namespace Arteranos.Audio
{
    /// <summary>
    /// An <see cref="IAudioInput"/> implementation based on UniMic.
    /// For more on UniMic, visit https://www.github.com/adrenak/unimic
    /// </summary>
    public class UVMicInput : IAudioInputV2
    {
        public event Action<int, byte[]> OnSegmentReady;

        public int Frequency => Mic.Instance.Frequency;

        public int ChannelCount => Mic.Instance.AudioClip?.channels ?? 0;

        public int SegmentRate => 1000 / Mic.Instance.SampleDurationMS;

        public UVMicInput(int deviceIndex = 0, int frequency = 16000, int sampleLen = 100)
        {
            if(Mic.Instance.Devices.Count == 0)
                throw new Exception("Must have recording devices for Microphone input");
            Mic.Instance.SetDeviceIndex(deviceIndex);
            Mic.Instance.StartRecording(frequency, sampleLen);
            //Mic.Instance.OnSampleReady += Mic_OnSampleReady;
        }

        void Mic_OnSampleReady(int segmentIndex, byte[] samples) => OnSegmentReady?.Invoke(segmentIndex, samples);

        public void Dispose()
        { }
        //=> Mic.Instance.OnSampleReady -= Mic_OnSampleReady;
    }
}
