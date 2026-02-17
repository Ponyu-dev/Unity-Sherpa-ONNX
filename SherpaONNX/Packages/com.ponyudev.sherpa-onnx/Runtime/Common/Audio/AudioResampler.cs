using System;

namespace PonyuDev.SherpaOnnx.Common.Audio
{
    /// <summary>
    /// Resamples PCM float arrays between sample rates using
    /// linear interpolation. Stateless, allocation-minimal.
    /// </summary>
    public static class AudioResampler
    {
        /// <summary>
        /// Resamples <paramref name="source"/> from
        /// <paramref name="sourceSampleRate"/> to
        /// <paramref name="targetSampleRate"/> using linear interpolation.
        /// Returns the original array when both rates match.
        /// </summary>
        public static float[] Resample(
            float[] source, int sourceSampleRate, int targetSampleRate)
        {
            if (source == null || source.Length == 0)
                return Array.Empty<float>();

            if (sourceSampleRate == targetSampleRate)
                return source;

            double ratio = (double)sourceSampleRate / targetSampleRate;
            int outputLength = (int)Math.Ceiling(source.Length / ratio);
            var output = new float[outputLength];

            for (int i = 0; i < outputLength; i++)
            {
                double srcIndex = i * ratio;
                int lo = (int)srcIndex;
                int hi = Math.Min(lo + 1, source.Length - 1);
                float frac = (float)(srcIndex - lo);

                output[i] = source[lo] + (source[hi] - source[lo]) * frac;
            }

            return output;
        }

        /// <summary>
        /// Downmixes multi-channel audio to mono by averaging channels,
        /// then resamples to <paramref name="targetSampleRate"/>.
        /// </summary>
        public static float[] ResampleMono(
            float[] source, int channels,
            int sourceSampleRate, int targetSampleRate)
        {
            if (source == null || source.Length == 0)
                return Array.Empty<float>();

            if (channels <= 1)
                return Resample(source, sourceSampleRate, targetSampleRate);

            int monoLength = source.Length / channels;
            var mono = new float[monoLength];

            for (int i = 0; i < monoLength; i++)
            {
                float sum = 0f;
                int baseIdx = i * channels;

                for (int ch = 0; ch < channels; ch++)
                    sum += source[baseIdx + ch];

                mono[i] = sum / channels;
            }

            return Resample(mono, sourceSampleRate, targetSampleRate);
        }
    }
}
