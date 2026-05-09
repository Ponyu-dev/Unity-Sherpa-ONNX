using System;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Static channel through which the VAD <c>SampleNavigator</c>
    /// publishes init progress for the VAD model and the pipeline's
    /// ASR companion, and any open sample panel subscribes to refresh
    /// its bottom status label in real time. Static so panels can
    /// reach it without changing their <c>Bind</c> signature.
    /// </summary>
    internal static class VadInitProgressBus
    {
        public static event Action Changed;

        private static float _vadFraction;
        private static float _asrFraction;
        private static bool _vadFinished;
        private static bool _asrFinished;

        public static int VadPercent =>
            Mathf.Clamp((int)(_vadFraction * 100f), 0, 100);

        public static int AsrPercent =>
            Mathf.Clamp((int)(_asrFraction * 100f), 0, 100);

        public static bool VadFinished => _vadFinished;
        public static bool AsrFinished => _asrFinished;

        public static void ReportVad(float fraction)
        {
            _vadFraction = fraction;
            Changed?.Invoke();
        }

        public static void ReportAsr(float fraction)
        {
            _asrFraction = fraction;
            Changed?.Invoke();
        }

        public static void MarkVadFinished()
        {
            _vadFraction = 1f;
            _vadFinished = true;
            Changed?.Invoke();
        }

        public static void MarkAsrFinished()
        {
            _asrFraction = 1f;
            _asrFinished = true;
            Changed?.Invoke();
        }
    }
}
