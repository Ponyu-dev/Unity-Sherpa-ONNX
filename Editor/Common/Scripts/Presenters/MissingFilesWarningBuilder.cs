using System;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.Common.Presenters
{
    /// <summary>
    /// Adds a HelpBox warning when model files are missing for a profile.
    /// Shared by all detail presenters (ASR, TTS, VAD).
    /// </summary>
    internal static class MissingFilesWarningBuilder
    {
        internal static void Build(VisualElement container, string profileName, Func<string, string> getModelDir)
        {
            if (!ModelFileService.IsProfileMissing(profileName, getModelDir)) return;
            container.Add(new HelpBox("Model files not found. The model directory may have been deleted. You can remove this profile or re-import the model.", HelpBoxMessageType.Error));
        }
    }
}
