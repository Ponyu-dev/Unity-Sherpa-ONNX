using System;

namespace PonyuDev.SherpaOnnx.Common.Audio
{
    /// <summary>
    /// Stateless PCM sample-rate conversion used by
    /// <see cref="MicrophoneSource"/> when the device's native rate
    /// (often 44.1/48 kHz on Android) does not match the requested
    /// target rate (typically 16 kHz for ASR/VAD).
    /// </summary>
    internal static class Resampler
    {
        public static float[] Resample(float[] input, int inputRate, int outputRate, ResamplingMode mode)
        {
            if (input == null || input.Length == 0)
                return input;
            if (inputRate <= 0 || outputRate <= 0 || inputRate == outputRate)
                return input;

            float[] filtered = mode == ResamplingMode.Lowpass
                ? LowPass(input, inputRate, outputRate)
                : input;

            return Linear(filtered, inputRate, outputRate);
        }

        // Linear interpolation. Output length = input.Length * outputRate / inputRate.
        private static float[] Linear(float[] input, int inputRate, int outputRate)
        {
            double ratio = (double)inputRate / outputRate;
            int outputLen = (int)(input.Length / ratio);
            if (outputLen <= 0)
                return Array.Empty<float>();

            var output = new float[outputLen];
            int last = input.Length - 1;

            for (int i = 0; i < outputLen; i++)
            {
                double srcPos = i * ratio;
                int srcIdx = (int)srcPos;
                if (srcIdx >= last)
                {
                    output[i] = input[last];
                    continue;
                }
                double frac = srcPos - srcIdx;
                output[i] = (float)(input[srcIdx] * (1.0 - frac) + input[srcIdx + 1] * frac);
            }

            return output;
        }

        // Single-pole RC low-pass. Cutoff at the target Nyquist (outputRate / 2).
        private static float[] LowPass(float[] input, int inputRate, int outputRate)
        {
            double cutoff = outputRate / 2.0;
            double rc = 1.0 / (2.0 * Math.PI * cutoff);
            double dt = 1.0 / inputRate;
            double alpha = dt / (rc + dt);

            var output = new float[input.Length];
            output[0] = input[0];
            for (int i = 1; i < input.Length; i++)
                output[i] = (float)(output[i - 1] + alpha * (input[i] - output[i - 1]));

            return output;
        }
    }
}
