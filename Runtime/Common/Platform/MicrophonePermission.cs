using Cysharp.Threading.Tasks;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace PonyuDev.SherpaOnnx.Common.Platform
{
    /// <summary>
    /// Cross-platform microphone permission helper.
    /// Handles Android <see cref="Permission"/>, iOS
    /// <see cref="Application.RequestUserAuthorization"/>,
    /// and desktop (always granted).
    /// </summary>
    public static class MicrophonePermission
    {
        /// <summary>
        /// Returns <c>true</c> when the application already has
        /// microphone permission on the current platform.
        /// </summary>
        public static bool HasPermission()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return Permission.HasUserAuthorizedPermission(
                Permission.Microphone);
#elif UNITY_IOS && !UNITY_EDITOR
            return Application.HasUserAuthorization(
                UserAuthorization.Microphone);
#else
            return true;
#endif
        }

        /// <summary>
        /// Requests microphone permission asynchronously.
        /// Returns <c>true</c> when granted, <c>false</c> when denied.
        /// On desktop/Editor returns <c>true</c> immediately.
        /// </summary>
        public static UniTask<bool> RequestAsync()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return RequestAndroidAsync();
#elif UNITY_IOS && !UNITY_EDITOR
            return RequestIosAsync();
#else
            SherpaOnnxLog.RuntimeLog(
                "[SherpaOnnx] Microphone permission: auto-granted (Editor/Desktop).");
            return UniTask.FromResult(true);
#endif
        }

        // ── Android ──

#if UNITY_ANDROID && !UNITY_EDITOR
        private static UniTaskCompletionSource<bool> _androidTcs;
        private static PermissionCallbacks _androidCallbacks;

        private static UniTask<bool> RequestAndroidAsync()
        {
            if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                SherpaOnnxLog.RuntimeLog(
                    "[SherpaOnnx] Microphone permission: already granted (Android).");
                return UniTask.FromResult(true);
            }

            _androidTcs = new UniTaskCompletionSource<bool>();
            _androidCallbacks = new PermissionCallbacks();

            _androidCallbacks.PermissionGranted += OnAndroidGranted;
            _androidCallbacks.PermissionDenied += OnAndroidDenied;
            _androidCallbacks.PermissionDeniedAndDontAskAgain += OnAndroidDeniedPermanently;

            Permission.RequestUserPermission(Permission.Microphone, _androidCallbacks);
            return _androidTcs.Task;
        }

        private static void UnsubscribeAndroidCallbacks()
        {
            if (_androidCallbacks == null)
                return;
            _androidCallbacks.PermissionGranted -= OnAndroidGranted;
            _androidCallbacks.PermissionDenied -= OnAndroidDenied;
            _androidCallbacks.PermissionDeniedAndDontAskAgain -= OnAndroidDeniedPermanently;
            _androidCallbacks = null;
        }

        private static void OnAndroidGranted(string permission)
        {
            UnsubscribeAndroidCallbacks();
            SherpaOnnxLog.RuntimeLog(
                "[SherpaOnnx] Microphone permission granted (Android).");
            _androidTcs?.TrySetResult(true);
        }

        private static void OnAndroidDenied(string permission)
        {
            UnsubscribeAndroidCallbacks();
            SherpaOnnxLog.RuntimeWarning(
                "[SherpaOnnx] Microphone permission denied (Android).");
            _androidTcs?.TrySetResult(false);
        }

        private static void OnAndroidDeniedPermanently(string permission)
        {
            UnsubscribeAndroidCallbacks();
            SherpaOnnxLog.RuntimeWarning(
                "[SherpaOnnx] Microphone permission denied permanently (Android).");
            _androidTcs?.TrySetResult(false);
        }
#endif

        // ── iOS ──

#if UNITY_IOS && !UNITY_EDITOR
        private static async UniTask<bool> RequestIosAsync()
        {
            if (Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                SherpaOnnxLog.RuntimeLog(
                    "[SherpaOnnx] Microphone permission: already granted (iOS).");
                return true;
            }

            await Application.RequestUserAuthorization(
                UserAuthorization.Microphone);

            bool granted = Application.HasUserAuthorization(
                UserAuthorization.Microphone);

            if (granted)
            {
                SherpaOnnxLog.RuntimeLog(
                    "[SherpaOnnx] Microphone permission granted (iOS).");
            }
            else
            {
                SherpaOnnxLog.RuntimeWarning(
                    "[SherpaOnnx] Microphone permission denied (iOS).");
            }

            return granted;
        }
#endif
    }
}
