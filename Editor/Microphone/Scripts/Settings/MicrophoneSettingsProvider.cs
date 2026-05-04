using PonyuDev.SherpaOnnx.Editor.Microphone.View;
using UnityEditor;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.Microphone.Settings
{
    internal static class MicrophoneSettingsProvider
    {
        private const string ProviderPath = "Project/Sherpa-ONNX/Microphone";
        private const string ProviderLabel = "Microphone";

        private const string UxmlPath =
            "Packages/com.ponyudev.sherpa-onnx/" +
            "Editor/Microphone/UI/MicrophoneSettings.uxml";

        [SettingsProvider]
        private static SettingsProvider CreateProvider()
        {
            var provider = new SettingsProvider(ProviderPath,
                SettingsScope.Project)
            {
                label = ProviderLabel,
                activateHandler = Activate,
                deactivateHandler = Deactivate
            };

            return provider;
        }

        private static MicrophoneSettingsView _view;

        private static void Activate(
            string searchContext, VisualElement rootElement)
        {
            _view = new MicrophoneSettingsView(uxmlPath: UxmlPath);
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
