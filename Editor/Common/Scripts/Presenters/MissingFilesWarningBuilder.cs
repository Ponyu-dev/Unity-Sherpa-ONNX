using System;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.Common.Presenters
{
    /// <summary>
    /// Adds a HelpBox warning when model files are missing for a profile.
    /// Returns a redownload <see cref="Button"/> when both files are missing
    /// and <paramref name="hasSourceUrl"/> is true; otherwise returns null.
    /// Shared by all detail presenters (ASR, TTS, VAD).
    /// </summary>
    internal static class MissingFilesWarningBuilder
    {
        internal static Button Build(VisualElement container, string profileName, Func<string, string> getModelDir, bool hasSourceUrl = false)
        {
            if (!ModelFileService.IsProfileMissing(profileName, getModelDir)) return null;

            string message = hasSourceUrl
                ? "Model files not found. Click 'Download & Setup' to re-download from the original URL."
                : "Model files not found. The model directory may have been deleted. You can remove this profile or re-import the model.";

            container.Add(new HelpBox(message, HelpBoxMessageType.Error));

            if (!hasSourceUrl) return null;

            var button = new Button { text = "Download & Setup" };
            button.AddToClassList("btn");
            button.AddToClassList("btn-primary");
            button.AddToClassList("model-btn-spaced");
            container.Add(button);
            return button;
        }
    }
}
