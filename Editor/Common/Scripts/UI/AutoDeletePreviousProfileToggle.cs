namespace PonyuDev.SherpaOnnx.Editor.Common.UI
{
    /// <summary>
    /// Shared label + tooltip for the "Auto-delete previous profile on
    /// switch" toggle used by TTS / ASR / VAD / Online ASR settings
    /// panels. Each panel constructs its own <c>Toggle</c> with a named
    /// handler so it can <c>UnregisterValueChangedCallback</c> on dispose
    /// — the constants here keep wording in sync.
    /// </summary>
    internal static class AutoDeletePreviousProfileToggle
    {
        public const string Label = "Auto-delete previous profile on switch";

        public const string Tooltip =
            "When ON, switching to a different profile via SwitchProfile " +
            "deletes the previously active profile's extracted directory " +
            "from persistentDataPath, freeing disk space. Applies to all " +
            "model sources (Local, Remote, LocalZip) — on Android the " +
            "plugin extracts each profile's files lazily into a dedicated " +
            "directory regardless of source. Only runs after the new " +
            "profile loads successfully — a failed switch leaves the " +
            "previous extraction intact. Default OFF: models stay on disk " +
            "so a re-switch does not pay the re-extract cost. On non-Android " +
            "platforms nothing is extracted, so this is a no-op.";
    }
}
