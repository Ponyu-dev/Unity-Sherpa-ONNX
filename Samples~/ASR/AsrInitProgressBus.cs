using System;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Static channel through which the ASR <c>SampleNavigator</c>
    /// publishes init progress for both the offline and online
    /// services, and any open sample panel subscribes to refresh its
    /// bottom status label in real time. Static so panels can reach
    /// it without changing their <c>Bind</c> signature.
    /// </summary>
    internal static class AsrInitProgressBus
    {
        public static event Action Changed;

        private static float _offlineFraction;
        private static float _onlineFraction;
        private static bool _offlineFinished;
        private static bool _onlineFinished;

        public static int OfflinePercent =>
            Mathf.Clamp((int)(_offlineFraction * 100f), 0, 100);

        public static int OnlinePercent =>
            Mathf.Clamp((int)(_onlineFraction * 100f), 0, 100);

        public static bool OfflineFinished => _offlineFinished;
        public static bool OnlineFinished => _onlineFinished;

        public static void ReportOffline(float fraction)
        {
            _offlineFraction = fraction;
            Changed?.Invoke();
        }

        public static void ReportOnline(float fraction)
        {
            _onlineFraction = fraction;
            Changed?.Invoke();
        }

        public static void MarkOfflineFinished()
        {
            _offlineFraction = 1f;
            _offlineFinished = true;
            Changed?.Invoke();
        }

        public static void MarkOnlineFinished()
        {
            _onlineFraction = 1f;
            _onlineFinished = true;
            Changed?.Invoke();
        }
    }
}
