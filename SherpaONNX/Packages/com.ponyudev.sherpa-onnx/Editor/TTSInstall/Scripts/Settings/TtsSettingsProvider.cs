using PonyuDev.SherpaOnnx.Editor.TtsInstall.View;
using UnityEditor;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.TtsInstall.Settings
{
    internal static class TtsSettingsProvider
    {
        private const string ProviderPath = "Project/Sherpa-ONNX/TTS";
        private const string ProviderLabel = "TTS";
        private const string UxmlPath = "Packages/com.ponyudev.sherpa-onnx/Editor/TTSInstall/UI/TtsModelsSettings.uxml";

        [SettingsProvider]
        private static SettingsProvider CreateProvider()
        {
            var provider = new SettingsProvider(ProviderPath, SettingsScope.Project)
            {
                label = ProviderLabel,

                activateHandler = Activate,
                deactivateHandler = Deactivate
            };

            return provider;
        }

        private static TtsSettingsView _view;

        private static void Activate(string searchContext, VisualElement rootElement)
        {
            _view = new TtsSettingsView(uxmlPath: UxmlPath);
            _view.Build(rootElement);
        }

        private static void Deactivate()
        {
            if (_view == null) return;
            _view.Dispose();
            _view = null;
        }
    }
}