namespace PonyuDev.SherpaOnnx.Editor.Common.UI
{
    /// <summary>
    /// Shared label + tooltip for the "Keep only active profile on
    /// disk" toggle used by TTS / Offline ASR / Online ASR / VAD
    /// settings panels. Each panel constructs its own <c>Toggle</c>
    /// with a named handler so it can
    /// <c>UnregisterValueChangedCallback</c> on dispose — the
    /// constants here keep wording in sync.
    /// </summary>
    internal static class KeepOnlyActiveProfileToggle
    {
        public const string Label = "Keep only active profile on disk";

        public const string Tooltip =
            "When ON, the runtime keeps only the active profile's " +
            "extracted directory under persistentDataPath; every other " +
            "profile's extraction is removed after a successful " +
            "InitializeAsync and after every successful SwitchProfile. " +
            "Applies to all model sources (Local, Remote, LocalZip) — " +
            "they share the same per-profile path on disk. Only profiles " +
            "registered in this service's settings are considered, so " +
            "offline + online ASR don't fight over the shared asr-models/ " +
            "directory. Failed loads leave the previous on-disk state " +
            "intact so the user can recover. Default OFF: every model " +
            "extracted at any point stays on disk, trading space for " +
            "fast re-switches. Implied automatically when 'Only active " +
            "profile in build' is on. On non-Android platforms nothing " +
            "is extracted, so this is a no-op.";
    }
}
