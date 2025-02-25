﻿using System;
using FragLabs.Audio.Codecs.Opus;

namespace FragLabs.Audio.Codecs
{
    /// <summary>
    /// Opus audio decoder.
    /// </summary>
    public class OpusDecoder : IDisposable
    {
        private IntPtr _decoder;

        /// <summary>
        /// Creates a new Opus decoder.
        /// </summary>
        /// <param name="outputSampleRate">Sample rate to decode at (Hz). This must be one of 8000, 12000, 16000, 24000, or 48000.</param>
        /// <param name="outputChannels">Number of channels to decode.</param>
        /// <returns>A new <c>OpusDecoder</c>.</returns>
        public static OpusDecoder Create(int outputSampleRate, int outputChannels)
        {
            if (outputSampleRate != 8000 &&
                outputSampleRate != 12000 &&
                outputSampleRate != 16000 &&
                outputSampleRate != 24000 &&
                outputSampleRate != 48000)
            {
                throw new ArgumentOutOfRangeException(nameof(outputSampleRate));
            }

            if (outputChannels != 1 && outputChannels != 2)
            {
                throw new ArgumentOutOfRangeException(nameof(outputChannels));
            }

            IntPtr decoder = API.opus_decoder_create(outputSampleRate, outputChannels, out var error);
            if ((Error)error != Error.OK)
            {
                throw new Exception("Exception occured while creating decoder");
            }
            return new OpusDecoder(decoder, outputSampleRate, outputChannels);
        }


        private OpusDecoder(IntPtr decoder, int outputSamplingRate, int outputChannels)
        {
            _decoder = decoder;
            this.OutputSamplingRate = outputSamplingRate;
            this.OutputChannels = outputChannels;
            this.MaxDataBytes = 4000;
        }

        /// <summary>
        /// Produces PCM samples from Opus encoded data.
        /// </summary>
        /// <param name="inputOpusData">Opus encoded data to decode, null for dropped packet.</param>
        /// <param name="dataLength">Length of data to decode.</param>
        /// <param name="decodedLength">Set to the length of the decoded sample data.</param>
        /// <returns>PCM audio samples.</returns>
        public unsafe byte[] Decode(byte[] inputOpusData, int dataLength, out int decodedLength)
        {
            if (disposed)
                throw new ObjectDisposedException("OpusDecoder");

            var decoded = new byte[MaxDataBytes];
            int frameCount = FrameCount(MaxDataBytes);
            int length;
            fixed (byte* DecodedPtr = decoded)
            {
                var decodedPtr = new IntPtr((void*)DecodedPtr);

                length = inputOpusData != null 
                    ? API.opus_decode(this._decoder, inputOpusData, dataLength, decodedPtr, frameCount, 0) 
                    : API.opus_decode(this._decoder, data: null, len: 0, pcm: decodedPtr, frame_size: frameCount, decode_fec:(this.ForwardErrorCorrection) ? 1 : 0);
            }
            decodedLength = length * 2;
            if (length < 0)
                throw new Exception("Decoding failed - " + ((Error)length).ToString());

            return decoded;
        }

        /// <summary>
        /// Determines the number of frames that can fit into a buffer of the given size.
        /// </summary>
        /// <param name="bufferSize"></param>
        /// <returns></returns>
        public int FrameCount(int bufferSize)
        {
            //  seems like bitrate should be required
            int bitrate = 16;
            int bytesPerSample = (bitrate / 8) * OutputChannels;
            return bufferSize / bytesPerSample;
        }

        /// <summary>
        /// Gets the output sampling rate of the decoder.
        /// </summary>
        public int OutputSamplingRate { get; private set; }

        /// <summary>
        /// Gets the number of channels of the decoder.
        /// </summary>
        public int OutputChannels { get; private set; }

        /// <summary>
        /// Gets or sets the size of memory allocated for decoding data.
        /// </summary>
        public int MaxDataBytes { get; set; }

        /// <summary>
        /// Gets or sets whether forward error correction is enabled or not.
        /// </summary>
        public bool ForwardErrorCorrection { get; set; }

        ~OpusDecoder()
        {
            Dispose();
        }

        private bool disposed;
        public void Dispose()
        {
            if (disposed)
                return;

            GC.SuppressFinalize(this);

            if (_decoder != IntPtr.Zero)
            {
                API.opus_decoder_destroy(_decoder);
                _decoder = IntPtr.Zero;
            }

            disposed = true;
        }
    }
}
