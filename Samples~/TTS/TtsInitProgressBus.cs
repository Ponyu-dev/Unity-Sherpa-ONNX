using System;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Static channel through which the TTS <c>SampleNavigator</c>
    /// publishes init progress, and any open sample panel subscribes
    /// to refresh its bottom status label in real time. Static so
    /// panels can reach it without changing their <c>Bind</c>
    /// signature — panels are short-lived UI controllers, the bus
    /// outlives them.
    /// </summary>
    internal static class TtsInitProgressBus
    {
        /// <summary>Fires whenever <see cref="Report"/> or
        /// <see cref="MarkFinished"/> is called. Subscribers should
        /// read <see cref="CurrentPercent"/> / <see cref="HasFinished"/>
        /// from this class — the event is parameterless so the
        /// callback can be a normal named method.</summary>
        public static event Action Changed;

        private static float _currentFraction;
        private static bool _hasFinished;

        public static int CurrentPercent =>
            Mathf.Clamp((int)(_currentFraction * 100f), 0, 100);

        public static bool HasFinished => _hasFinished;

        public static void Report(float fraction)
        {
            _currentFraction = fraction;
            Changed?.Invoke();
        }

        public static void MarkFinished()
        {
            _currentFraction = 1f;
            _hasFinished = true;
            Changed?.Invoke();
        }
    }
}
