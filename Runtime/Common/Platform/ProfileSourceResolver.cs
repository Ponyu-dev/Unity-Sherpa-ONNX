using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common.Data;
using PonyuDev.SherpaOnnx.Common.Networking;
using UnityEngine;

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
    ///
    /// Editor override: in <c>Application.isEditor</c> any non-Local
    /// source is treated as Local — no zip extraction, no download.
    /// The Editor always reads model files from StreamingAssets, since
    /// downloading 100 MB+ archives every PlayMode is wasteful and
    /// LocalZip's build-time zip artifact does not exist yet. The path
    /// resolvers in TtsModelPathResolver / AsrModelPathResolver /
    /// VadModelPathResolver mirror this and route every source to the
    /// StreamingAssets-relative directory in Editor.
    /// </summary>
    public static class ProfileSourceResolver
    {
        /// <summary>
        /// Extracts the bundled <c>.zip</c> archive for a
        /// <see cref="ModelSource.LocalZip"/> profile. No-op for any
        /// other source. In Editor the zip artifact does not exist
        /// (it is created at build time), so this is also a no-op
        /// there — files are read straight from StreamingAssets.
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

            if (Application.isEditor)
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
        ///
        /// In Editor this is also a no-op — the model files are
        /// expected in StreamingAssets (typically placed there by the
        /// Editor "Import from URL" importer). If they are not there
        /// LoadProfile will fail with a missing-file error, prompting
        /// the developer to import the model into the project rather
        /// than re-download every PlayMode.
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

            if (Application.isEditor)
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
