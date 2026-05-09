namespace PonyuDev.SherpaOnnx.Editor.Common.UI
{
    /// <summary>
    /// Shared label + tooltip for the "Only active profile in build"
    /// toggle used by TTS / Offline ASR / Online ASR / VAD settings
    /// panels. When enabled, the build pipeline temporarily moves every
    /// non-active profile's model files out of StreamingAssets before
    /// manifest generation so the produced build only ships the active
    /// profile. Each panel constructs its own <c>Toggle</c> with a named
    /// handler so it can <c>UnregisterValueChangedCallback</c> on
    /// dispose — the constants here keep wording in sync.
    /// </summary>
    internal static class OnlyActiveProfileInBuildToggle
    {
        public const string Label = "Only active profile in build";

        public const string Tooltip =
            "When ON, the build pipeline temporarily moves every " +
            "non-active profile's model directory (and any LocalZip " +
            "archive) out of StreamingAssets before the manifest is " +
            "generated, so the produced build only ships the active " +
            "profile's files. Files are restored after the build " +
            "finishes; a defensive restore on Editor reload covers " +
            "crashes or cancellations. The active profile is taken " +
            "from \"Active Profile\" — if none is set the toggle is a " +
            "no-op and a warning is logged. Default OFF: every profile " +
            "ships into the build.";
    }
}
