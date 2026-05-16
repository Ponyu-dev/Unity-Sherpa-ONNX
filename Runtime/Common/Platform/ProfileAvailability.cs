using System.IO;
using PonyuDev.SherpaOnnx.Common.Data;

namespace PonyuDev.SherpaOnnx.Common.Platform
{
    /// <summary>
    /// Per-profile reachability check used by service-level
    /// <c>IsProfileAvailable</c> implementations. Treats:
    /// <list type="bullet">
    ///   <item><c>Remote</c> profile + non-empty <c>SourceUrl</c> as
    ///         always available — the runtime will download on switch.</item>
    ///   <item><c>Local</c> / <c>LocalZip</c> profile as available if
    ///         the directory the path resolver would feed into the
    ///         native engine ctor exists on disk. On Editor / Standalone
    ///         that means files in StreamingAssets; on Android Player
    ///         that means a previously-extracted folder under
    ///         persistentDataPath.</item>
    /// </list>
    /// Conservative on Android: a Local profile that is in the build
    /// manifest but has not been lazy-extracted yet returns false.
    /// Switching to it would still succeed (the resolver triggers
    /// extraction), but a UI dropdown is better off hiding entries
    /// whose membership we cannot quickly verify than showing one
    /// that might fail.
    /// </summary>
    public static class ProfileAvailability
    {
        /// <summary>
        /// Returns whether the given profile is reachable on the
        /// current platform. <paramref name="modelDir"/> is the
        /// platform-correct path the service's path resolver would
        /// produce for this profile (e.g.
        /// <c>TtsModelPathResolver.GetModelDirectory(profileName, source)</c>).
        /// </summary>
        public static bool IsAvailable(IModelProfile profile, string modelDir)
        {
            if (profile == null || string.IsNullOrEmpty(profile.ProfileName))
                return false;

            switch (profile.ModelSource)
            {
                case ModelSource.Remote:
                    return !string.IsNullOrEmpty(profile.SourceUrl)
                           || (!string.IsNullOrEmpty(modelDir) && Directory.Exists(modelDir));

                case ModelSource.Local:
                case ModelSource.LocalZip:
                    return !string.IsNullOrEmpty(modelDir) && Directory.Exists(modelDir);

                default:
                    return false;
            }
        }
    }
}
