using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using POpusCodec.Enums;

namespace POpusCodec
{
    internal class Wrapper
    {
        // --------------------------------------------------------------------
        #region DLL Imports

        [DllImport("opus", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int opus_encoder_get_size(Channels channels);

        [DllImport("opus", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern OpusStatusCode opus_encoder_init(IntPtr st, SamplingRate Fs, Channels channels, OpusApplicationType application);

        [DllImport("opus", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern string opus_get_version_string();

        [DllImport("opus", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int opus_encoder_ctl(IntPtr st, OpusCtlSetRequest request, int value);

        [DllImport("opus", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int opus_encoder_ctl(IntPtr st, OpusCtlGetRequest request, ref int value);

        [DllImport("opus", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int opus_decoder_get_size(Channels channels);

        [DllImport("opus", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern OpusStatusCode opus_decoder_init(IntPtr st, SamplingRate Fs, Channels channels);

        [DllImport("opus", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int opus_decode(IntPtr st, byte[] data, int len, short[] pcm, int frame_size, int decode_fec);

        [DllImport("opus", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int opus_decode_float(IntPtr st, byte[] data, int len, float[] pcm, int frame_size, int decode_fec);

        [DllImport("opus", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int opus_decode(IntPtr st, IntPtr data, int len, short[] pcm, int frame_size, int decode_fec);

        [DllImport("opus", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int opus_decode_float(IntPtr st, IntPtr data, int len, float[] pcm, int frame_size, int decode_fec);

        [DllImport("opus", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int opus_packet_get_bandwidth(byte[] data);

        [DllImport("opus", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int opus_packet_get_nb_channels(byte[] data);

        [DllImport("opus", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern string opus_strerror(OpusStatusCode error);

        [DllImport("opus", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int opus_encode(IntPtr st, short[] pcm, int frame_size, byte[] data, int max_data_bytes);

        [DllImport("opus", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int opus_encode_float(IntPtr st, float[] pcm, int frame_size, byte[] data, int max_data_bytes);

        #endregion

        public static IntPtr Opus_encoder_create(SamplingRate Fs, Channels channels, OpusApplicationType application)
        {
            int size = Wrapper.opus_encoder_get_size(channels);
            IntPtr ptr = Marshal.AllocHGlobal(size);

            OpusStatusCode statusCode = Wrapper.opus_encoder_init(ptr, Fs, channels, application);

            try
            {
                HandleStatusCode(statusCode);
            }
            catch (Exception ex)
            {
                if(ptr != IntPtr.Zero) Opus_encoder_destroy(ptr);

                throw ex;
            }

            return ptr;
        }
        public static int Opus_encode(IntPtr st, short[] pcm, int frame_size, byte[] data)
        {
            if (st == IntPtr.Zero)
                throw new ObjectDisposedException("OpusEncoder");

            int payloadLength = opus_encode(st, pcm, frame_size, data, data.Length);

            if (payloadLength <= 0)
            {
                HandleStatusCode((OpusStatusCode)payloadLength);
            }

            return payloadLength;
        }

        public static int Opus_encode(IntPtr st, float[] pcm, int frame_size, byte[] data)
        {
            if (st == IntPtr.Zero)
                throw new ObjectDisposedException("OpusEncoder");

            int payloadLength = opus_encode_float(st, pcm, frame_size, data, data.Length);

            if (payloadLength <= 0)
            {
                HandleStatusCode((OpusStatusCode)payloadLength);
            }

            return payloadLength;
        }

        public static void Opus_encoder_destroy(IntPtr st) => Marshal.FreeHGlobal(st);

        public static int Get_opus_encoder_ctl(IntPtr st, OpusCtlGetRequest request)
        {
            if (st == IntPtr.Zero)
                throw new ObjectDisposedException("OpusEncoder");

            int value = 0;
            OpusStatusCode statusCode = (OpusStatusCode)opus_encoder_ctl(st, request, ref value);

            HandleStatusCode(statusCode);

            return value;
        }

        public static void Set_opus_encoder_ctl(IntPtr st, OpusCtlSetRequest request, int value)
        {
            if (st == IntPtr.Zero)
                throw new ObjectDisposedException("OpusEncoder");

            OpusStatusCode statusCode = (OpusStatusCode)opus_encoder_ctl(st, request, value);

            HandleStatusCode(statusCode);
        }

        public static IntPtr Opus_decoder_create(SamplingRate Fs, Channels channels)
        {
            int size = Wrapper.opus_decoder_get_size(channels);
            IntPtr ptr = Marshal.AllocHGlobal(size);

            OpusStatusCode statusCode = Wrapper.opus_decoder_init(ptr, Fs, channels);

            try
            {
                HandleStatusCode(statusCode);
            }
            catch (Exception ex)
            {
                if (ptr != IntPtr.Zero) Opus_decoder_destroy(ptr);

                throw ex;
            }

            return ptr;
        }

        public static void Opus_decoder_destroy(IntPtr st) => Marshal.FreeHGlobal(st);




        public static int Opus_decode(IntPtr st, byte[] data, short[] pcm, int decode_fec, int channels)
        {
            if (st == IntPtr.Zero)
                throw new ObjectDisposedException("OpusDecoder");

            int numSamplesDecoded;
            if(data != null)
            {
                numSamplesDecoded = opus_decode(st, data, data.Length, pcm, pcm.Length / channels, decode_fec);
            }
            else
            {
                numSamplesDecoded = opus_decode(st, IntPtr.Zero, 0, pcm, pcm.Length / channels, decode_fec);
            }

            if (numSamplesDecoded == (int)OpusStatusCode.InvalidPacket)
                return 0;

            if (numSamplesDecoded <= 0)
            {
                HandleStatusCode((OpusStatusCode)numSamplesDecoded);
            }

            return numSamplesDecoded;
        }

        public static int Opus_decode(IntPtr st, byte[] data, float[] pcm, int decode_fec, int channels)
        {
            if (st == IntPtr.Zero)
                throw new ObjectDisposedException("OpusDecoder");

            int numSamplesDecoded;
            if(data != null)
            {
                numSamplesDecoded = opus_decode_float(st, data, data.Length, pcm, pcm.Length / channels, decode_fec);
            }
            else
            {
                numSamplesDecoded = opus_decode_float(st, IntPtr.Zero, 0, pcm, pcm.Length / channels, decode_fec);
            }

            if (numSamplesDecoded == (int)OpusStatusCode.InvalidPacket)
                return 0;

            if (numSamplesDecoded <= 0)
            {
                HandleStatusCode((OpusStatusCode)numSamplesDecoded);
            }

            return numSamplesDecoded;
        }

        private static void HandleStatusCode(OpusStatusCode statusCode)
        {
            if (statusCode != OpusStatusCode.OK)
            {
                throw new OpusException(statusCode, opus_strerror(statusCode));
            }
        }
    }
}
