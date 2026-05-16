using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.Common.Presenters
{
    /// <summary>
    /// Shared UI builders for model source sections (LocalZip, Remote).
    /// Used by all detail presenters (TTS, ASR, VAD) to avoid duplication.
    /// </summary>
    internal static class ModelSourceSectionBuilder
    {
        // ── LocalZip ──

        internal struct LocalZipResult
        {
            internal Button PackButton;
            internal Button DeleteButton;
        }

        /// <summary>
        /// Builds the LocalZip info label and a Pack or Delete button.
        /// Returns a result struct — the presenter subscribes via method groups.
        /// </summary>
        internal static LocalZipResult BuildLocalZip(VisualElement container, string modelDir)
        {
            var infoLabel = new Label(
                "Model files will be zipped at build time and extracted " +
                "from StreamingAssets to persistentDataPath on first launch.");
            infoLabel.AddToClassList("model-info-label");
            container.Add(infoLabel);

            var result = new LocalZipResult();

            if (!ModelFileService.ModelDirExists(modelDir))
                return result;

            if (ModelFileService.ZipExists(modelDir))
            {
                var deleteButton = new Button { text = "Delete zip" };
                deleteButton.AddToClassList("btn");
                deleteButton.AddToClassList("btn-secondary");
                container.Add(deleteButton);
                result.DeleteButton = deleteButton;
            }
            else
            {
                var packButton = new Button { text = "Pack to zip (test)" };
                packButton.AddToClassList("btn");
                packButton.AddToClassList("btn-primary");
                container.Add(packButton);
                result.PackButton = packButton;
            }

            return result;
        }

        // ── Remote ──

        /// <summary>
        /// Creates a Label showing the computed archive URL preview.
        /// </summary>
        internal static Label BuildArchiveUrlPreview(string baseUrl, string profileName)
        {
            string archiveUrl = string.IsNullOrEmpty(baseUrl)
                ? "(set Base URL first)"
                : baseUrl.TrimEnd('/') + "/" + profileName + ".zip";

            var label = new Label("Archive URL: " + archiveUrl);
            label.AddToClassList("model-url-preview");
            return label;
        }
    }
}
