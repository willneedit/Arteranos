using System;
using System.Collections;
using System.Collections.Generic;
using Adrenak.UniVoice;

using POpusCodec;
using POpusCodec.Enums;
using UnityEngine;

namespace Arteranos.Audio
{
    /// <summary>
    /// An <see cref="IAudioInput"/> implementation based on UniMic.
    /// For more on UniMic, visit https://www.github.com/adrenak/unimic
    /// </summary>
    public class UVMicInput : MonoBehaviour, IAudioInputV2
    {
        public event Action<int, byte[]> OnSegmentReady;

        public AudioSource audiorecorder = null;
        private readonly List<float> micBuffer = new();
        int packageSize;

        int packageIndex = 0;

        OpusEncoder encoder;

        private bool recording = true;

        // other values then stereo will not yet work
        Channels opusChannels = Channels.Mono;
        // other values then 48000 do not work at the moment, it requires and additional conversion before sending and at receiving
        // also osx runs at 44100 i think, this causes also some hickups
        SamplingRate opusSamplingRate = SamplingRate.Sampling48000;

        // TODO Cleanup
        public int Frequency => 48000;
        public int ChannelCount => 1;
        public int SegmentRate => 4096;

        public string deviceName;

        private event Action<float[]> OnSampleReady;

        [Obsolete("Cannot use new keyword to create an instance. Use .New() method instead")]
        public UVMicInput() { }


        public static UVMicInput New(int micDeviceId = 0)
        {
            GameObject go = new("_Microphone");
            DontDestroyOnLoad(go);
            go.transform.SetParent(Core.SettingsManager.Purgatory);
            return go.AddComponent<UVMicInput>().New_(micDeviceId);
        }

        private UVMicInput New_(int micDeviceId)
        {
            encoder = new(opusSamplingRate, opusChannels);
            encoder.EncoderDelay = Delay.Delay20ms;
            Debug.Log("Opustest.Start: framesize: " + encoder.FrameSizePerChannel + " " + encoder.InputChannels);

            // the encoder delay has some influence on the amout of data we need to send, but it's not a multiplication of it
            packageSize = encoder.FrameSizePerChannel * (int) opusChannels;

            deviceName = Microphone.devices[micDeviceId];

            Debug.Log($"setup mic with {deviceName}, samplerate={AudioSettings.outputSampleRate}");
            audiorecorder = gameObject.AddComponent<AudioSource>();
            audiorecorder.loop = true;
            audiorecorder.clip = Microphone.Start(
                deviceName,
                true,
                1,
                AudioSettings.outputSampleRate);

            Debug.Log($"Clip sanples={audiorecorder.clip.samples}");

            OnSampleReady += DeliverCompressedAudio;
            StartCoroutine(ReadRawAudio());

            return this;
        }

        private void OnDestroy()
        {
            StopCoroutine(ReadRawAudio());
            OnSampleReady -= DeliverCompressedAudio;
        }

        IEnumerator ReadRawAudio()
        {
            int loops = 0;
            int readAbsPos = 0;
            int prevPos = 0;

            AudioClip AudioClip = audiorecorder.clip;

            int ClipDataLength = AudioSettings.outputSampleRate / 10;

            float[] temp = new float[ClipDataLength];

            while(true)
            {
                bool isNewDataAvailable = true;

                while(isNewDataAvailable)
                {
                    int currPos = Microphone.GetPosition(deviceName);
                    if(currPos < prevPos)
                        loops++;
                    prevPos = currPos;

                    var currAbsPos = loops * AudioClip.samples + currPos;
                    var nextReadAbsPos = readAbsPos + temp.Length;

                    if(nextReadAbsPos < currAbsPos)
                    {
                        AudioClip.GetData(temp, readAbsPos % AudioClip.samples);

                        OnSampleReady?.Invoke(temp);

                        readAbsPos = nextReadAbsPos;
                        isNewDataAvailable = true;
                    }
                    else
                        isNewDataAvailable = false;
                }
                yield return null;
            }
        }

        private void DeliverCompressedAudio(float[] data)
        {
            micBuffer.AddRange(data);

            while(micBuffer.Count > packageSize)
            {
                byte[] encodedData = encoder.Encode(micBuffer.GetRange(0, packageSize).ToArray());
                Debug.Log("OpusNetworked.SendData: " + encodedData.Length);
                micBuffer.RemoveRange(0, packageSize);
                OnSegmentReady?.Invoke(packageIndex++, encodedData);
            }
        }

        public void Dispose() => Destroy(audiorecorder.gameObject);
    }
}
