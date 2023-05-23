
namespace FragLabs.Audio.Codecs.Opus
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Wraps the Opus API.
    /// See https://opus-codec.org/docs/opus_api-1.3.1/group__opus__encoder.html#gaf461a3ef2f10c2fe8b994a176f06c9bd
    /// </summary>
    internal class API
    {
        /// <summary>
        /// Allocates and initializes an encoder state.
        /// </summary>
        /// <param name="Fs">Sampling rate of input signal (Hz) This must be one of 8000, 12000, 16000, 24000, or 48000.</param>
        /// <param name="channels">Number of channels (1 or 2) in input signal.</param>
        /// <param name="application">Coding mode. There are three coding modes:
        /// 
        ///    1) OPUS_APPLICATION_VOIP gives best quality at a given bitrate for voice signals.
        ///         It enhances the input signal by high-pass filtering and emphasizing formants and harmonics.
        ///         Optionally it includes in-band forward error correction to protect against packet loss.
        ///         Use this mode for typical VoIP applications.
        ///
        ///         Because of the enhancement, even at high bitrates the output may sound different from the input.
        /// 
        ///   2) OPUS_APPLICATION_AUDIO gives best quality at a given bitrate for most non-voice signals like music.
        ///      Use this mode for music and mixed (music/voice) content, broadcast, and applications
        ///      requiring less than 15 ms of coding delay.
        /// 
        ///   3) OPUS_APPLICATION_RESTRICTED_LOWDELAY configures low-delay mode that disables
        ///     the speech-optimized mode in exchange for slightly reduced delay.
        ///     This mode can only be set on an newly initialized or freshly reset encoder because it changes the codec delay.
        ///     This is useful when the caller knows that the speech-optimized modes will not be needed (use with caution).
        /// </param>
        /// <param name="error">Error codes.</param>
        /// <returns>
        ///     An OpusEncoder struct pointer. This contains the complete state of an Opus encoder.
        ///     It is position independent and can be freely copied.
        /// </returns>
        /// <remarks>
        ///     Regardless of the sampling rate and number channels selected, the Opus encoder can switch
        ///     to a lower audio bandwidth or number of channels if the bitrate selected is too low.
        ///
        ///     ✅ This also means that it is safe to always use 48 kHz stereo input and let the encoder optimize the encoding.
        /// </remarks>
        [DllImport("libopus.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr opus_encoder_create(int Fs, int channels, int application, out IntPtr error);

        /// <summary>
        /// Encodes an Opus frame.
        /// </summary>
        /// <param name="st"> (OpusEncoder*) Encoder state.</param>
        /// <param name="pcm">Input signal (interleaved if 2 channels). Length is frame_size * channels * sizeof(opus_int16)</param>
        /// <param name="frame_size">
        ///   Number of samples per channel in the input signal. This must be an Opus frame size for the encoder's
        ///   sampling rate.
        ///
        ///   For example, at 48 kHz the permitted values are 120, 240, 480, 960, 1920, and 2880.
        ///   Passing in a duration of less than 10 ms (480 samples at 48 kHz) will prevent the
        ///   encoder from using the LPC or hybrid modes.
        /// </param>
        /// <param name="data">[Out] Output payload. This must contain storage for at least <paramref name="max_data_bytes"/>.</param>
        /// <param name="max_data_bytes">
        ///  Size of the allocated memory for the output payload. This may be used to impose an upper limit on the instant bitrate, but should not be used as the only bitrate control. Use OPUS_SET_BITRATE to control the bitrate.
        /// </param>
        /// <returns></returns>
        [DllImport("libopus.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int opus_encode(IntPtr st, byte[] pcm, int frame_size, IntPtr data, int max_data_bytes);

        [DllImport("libopus.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int opus_encoder_ctl(IntPtr st, Ctl request, int value);

        [DllImport("libopus.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int opus_encoder_ctl(IntPtr st, Ctl request, out int value);

        [DllImport("libopus.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void opus_encoder_destroy(IntPtr encoder);

        [DllImport("libopus.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr opus_decoder_create(int Fs, int channels, out IntPtr error);

        [DllImport("libopus.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void opus_decoder_destroy(IntPtr decoder);

        [DllImport("libopus.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int opus_decode(IntPtr st, byte[] data, int len, IntPtr pcm, int frame_size, int decode_fec);
    }
}
