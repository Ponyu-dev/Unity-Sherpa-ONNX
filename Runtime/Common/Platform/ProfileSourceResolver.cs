using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common.Data;
using PonyuDev.SherpaOnnx.Common.Networking;

namespace PonyuDev.SherpaOnnx.Common.Platform
{
    /// <summary>
    /// Per-source readiness helpers for <see cref="IModelProfile"/>.
    /// Each method early-returns <c>true</c> when the profile's
    /// <see cref="IModelProfile.ModelSource"/> does not match the
    /// method's source — services can chain three independent calls
    /// (LocalZip / Remote / Local) instead of nesting if/else if/else.
    /// Returns <c>false</c> only when the matching source's preparation
    /// step actually fails. Each helper emits the
    /// <see cref="ProfileReadyPhase.Extract"/> phase (or
    /// <see cref="ProfileReadyPhase.Download"/> + Extract for Remote)
    /// with 0..100 percent through the supplied <c>onEvent</c>.
    /// </summary>
    public static class ProfileSourceResolver
    {
        /// <summary>
        /// Extracts the bundled <c>.zip</c> archive for a
        /// <see cref="ModelSource.LocalZip"/> profile. No-op for any
        /// other source.
        /// </summary>
        public static async UniTask<bool> EnsureLocalZipReadyAsync(
            IModelProfile profile,
            string modelsSubfolder,
            string serviceName,
            Action<ProfileReadyEvent> onEvent = null,
            CancellationToken ct = default)
        {
            if (profile == null || profile.ModelSource != ModelSource.LocalZip)
                return true;

            ProfileReadyEvents.Emit(onEvent, ProfileReadyPhase.Extract, 0);
            var bridge = ProfileReadyEvents.AsExtractProgress(onEvent);

            string dir = await LocalZipExtractor.EnsureExtractedAsync(modelsSubfolder, profile.ProfileName, bridge, ct);
            if (dir == null)
            {
                string msg = $"LocalZip extraction failed for '{profile.ProfileName}'.";
                SherpaOnnxLog.RuntimeError($"[SherpaOnnx] {serviceName}: {msg}");
                ProfileReadyEvents.EmitFailed(onEvent, msg);
                return false;
            }
            ProfileReadyEvents.Emit(onEvent, ProfileReadyPhase.Extract, 100);
            return true;
        }

        /// <summary>
        /// Downloads + extracts the runtime archive at
        /// <see cref="IModelProfile.SourceUrl"/> for a
        /// <see cref="ModelSource.Remote"/> profile. No-op for any
        /// other source. Forwards Download / Extract / Failed events
        /// directly from <see cref="RemoteProfileFetcher"/>.
        /// </summary>
        public static async UniTask<bool> EnsureRemoteReadyAsync(
            IModelProfile profile,
            string modelsSubfolder,
            string serviceName,
            Action<ProfileReadyEvent> onEvent = null,
            CancellationToken ct = default)
        {
            if (profile == null || profile.ModelSource != ModelSource.Remote)
                return true;

            bool ok = await RemoteProfileFetcher.EnsureDownloadedAsync(modelsSubfolder, profile.ProfileName, profile.SourceUrl, onEvent, ct);
            if (!ok)
            {
                SherpaOnnxLog.RuntimeError($"[SherpaOnnx] {serviceName}: remote download failed for '{profile.ProfileName}'.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// On Android lazily extracts the per-profile group from the
        /// APK for a <see cref="ModelSource.Local"/> profile. On other
        /// platforms this is a no-op (StreamingAssets is already on the
        /// filesystem). No-op for any other source.
        /// </summary>
        public static async UniTask<bool> EnsureLocalReadyAsync(
            IModelProfile profile,
            string modelsSubfolder,
            string serviceName,
            Action<ProfileReadyEvent> onEvent = null,
            CancellationToken ct = default)
        {
            if (profile == null || profile.ModelSource != ModelSource.Local)
                return true;

            ProfileReadyEvents.Emit(onEvent, ProfileReadyPhase.Extract, 0);
            var bridge = ProfileReadyEvents.AsExtractProgress(onEvent);

            string subdir = $"{modelsSubfolder}/{profile.ProfileName}";
            bool ok = await StreamingAssetsCopier.EnsureProfileExtractedAsync(subdir, bridge, ct);
            if (!ok)
            {
                string msg = $"Profile extraction failed for '{subdir}'.";
                SherpaOnnxLog.RuntimeError($"[SherpaOnnx] {serviceName}: {msg}");
                ProfileReadyEvents.EmitFailed(onEvent, msg);
                return false;
            }
            ProfileReadyEvents.Emit(onEvent, ProfileReadyPhase.Extract, 100);
            return true;
        }
    }
}
