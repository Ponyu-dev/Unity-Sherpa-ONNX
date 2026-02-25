using UnityEditor;

namespace PonyuDev.SherpaOnnx.Editor.AsrInstall.Import
{
    /// <summary>
    /// Validates whether an ASR model archive matches the import target tab
    /// (offline / online). Returns a warning message on mismatch, null if OK.
    /// </summary>
    internal static class AsrImportMismatchValidator
    {
        private static readonly string[] OfflineOnlyKeywords =
        {
            "whisper", "moonshine",
            "sense-voice", "sensevoice",
            "fire-red", "firered",
            "dolphin", "canary", "wenet",
            "omnilingual",
            "med-asr", "medasr",
            "fun-asr", "funasrnano",
            "tdnn"
        };

        /// <summary>
        /// Checks whether an archive imported into the Offline tab
        /// looks like a streaming (online) model.
        /// Returns a warning message or null if no mismatch detected.
        /// </summary>
        internal static string CheckOfflineImport(string archiveName)
        {
            if (string.IsNullOrEmpty(archiveName))
                return null;

            string lower = archiveName.ToLowerInvariant();

            if (lower.Contains("streaming"))
                return "This looks like a streaming (online) model. Are you sure you want to import it into the Offline tab?";

            return null;
        }

        /// <summary>
        /// Checks whether an archive imported into the Online tab
        /// looks like an offline-only model.
        /// Returns a warning message or null if no mismatch detected.
        /// </summary>
        internal static string CheckOnlineImport(string archiveName)
        {
            if (string.IsNullOrEmpty(archiveName))
                return null;

            string lower = archiveName.ToLowerInvariant();

            for (int i = 0; i < OfflineOnlyKeywords.Length; i++)
            {
                if (lower.Contains(OfflineOnlyKeywords[i]))
                    return $"'{OfflineOnlyKeywords[i]}' models are offline-only and will not work in the Online tab.";
            }

            return null;
        }

        // ── Convenience: check + dialog ──

        /// <summary>
        /// Checks mismatch for offline import and shows a confirmation dialog
        /// if needed. Returns true if import should proceed.
        /// </summary>
        internal static bool ConfirmOfflineImport(string archiveName)
        {
            return Confirm(CheckOfflineImport(archiveName));
        }

        /// <summary>
        /// Checks mismatch for online import and shows a confirmation dialog
        /// if needed. Returns true if import should proceed.
        /// </summary>
        internal static bool ConfirmOnlineImport(string archiveName)
        {
            return Confirm(CheckOnlineImport(archiveName));
        }

        private static bool Confirm(string warning)
        {
            if (warning == null) return true;
            return EditorUtility.DisplayDialog("Model type mismatch", warning, "Import anyway", "Cancel");
        }
    }
}
