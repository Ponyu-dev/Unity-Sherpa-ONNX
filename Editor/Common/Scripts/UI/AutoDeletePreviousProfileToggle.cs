namespace PonyuDev.SherpaOnnx.Editor.Common.UI
{
    /// <summary>
    /// Shared label + tooltip for the "Auto-delete previous LocalZip on
    /// switch" toggle used by TTS / ASR / VAD / Online ASR settings
    /// panels. Each panel constructs its own <c>Toggle</c> with a named
    /// handler so it can <c>UnregisterValueChangedCallback</c> on dispose
    /// — the constants here keep wording in sync.
    /// </summary>
    internal static class AutoDeletePreviousProfileToggle
    {
        public const string Label = "Auto-delete previous LocalZip on switch";

        public const string Tooltip =
            "When ON, switching to a different profile via SwitchProfile " +
            "deletes the extracted LocalZip directory of the previously " +
            "active profile from persistentDataPath, freeing disk space. " +
            "Only runs after the new profile loads successfully — a failed " +
            "switch leaves the previous extraction intact. Default OFF: " +
            "models stay on disk so a re-switch does not pay the re-extract " +
            "cost. Has no effect on Local or Remote profiles (only LocalZip " +
            "is extracted to persistentDataPath).";
    }
}
