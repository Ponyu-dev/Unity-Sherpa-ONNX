using System.Collections.Generic;

namespace PonyuDev.SherpaOnnx.Common.Platform
{
    /// <summary>
    /// Disk-management contract implemented by every model-owning
    /// service (TTS, offline ASR, online ASR, VAD). Lets host projects
    /// inspect and free the persistent storage used by extracted
    /// LocalZip profiles without knowing about
    /// <see cref="LocalZipExtractor"/> or per-service
    /// <c>ModelsSubfolder</c> constants.
    /// Local and Remote profiles are not stored under
    /// <see cref="UnityEngine.Application.persistentDataPath"/> by the
    /// plugin, so they never appear in <see cref="GetExtractedProfiles"/>.
    /// </summary>
    public interface IModelDiskUsage
    {
        /// <summary>
        /// Lists every profile name that has an extracted directory on
        /// disk for this service. Includes stale folders left over from
        /// renamed or removed profiles — useful as input for
        /// <see cref="CleanupUnusedExtractedProfiles"/>.
        /// </summary>
        IReadOnlyList<string> GetExtractedProfiles();

        /// <summary>
        /// Returns the size on disk (sum of all files) of the named
        /// extracted profile, or <c>0</c> if it is not extracted.
        /// </summary>
        long GetExtractedProfileSizeBytes(string profileName);

        /// <summary>
        /// Deletes the extracted directory for one profile. Returns
        /// <c>true</c> when the directory existed and was removed (or
        /// it was already absent). Logs and returns <c>false</c> on I/O
        /// failure.
        /// </summary>
        bool TryDeleteExtractedProfile(string profileName);

        /// <summary>
        /// Removes every extracted profile that is not currently listed
        /// in this service's settings. Returns the number of profiles
        /// deleted. Use to free space taken by orphan / renamed /
        /// removed profiles in one call.
        /// </summary>
        int CleanupUnusedExtractedProfiles();
    }
}
