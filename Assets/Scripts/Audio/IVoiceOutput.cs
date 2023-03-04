namespace Arteranos.Audio
{
    public interface IVoiceOutput
    {
        /// <summary>
        /// Measures the amplitude of the upcoming audio data
        /// </summary>
        /// <param name="integralDuration">Time in seconds to measure</param>
        /// <returns>Height of amplitude, ranged [0...1]</returns>
        float MeasureAmplitude();
    }
}