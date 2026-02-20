using PonyuDev.SherpaOnnx.Editor.AsrInstall.View;
using UnityEditor;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.AsrInstall.Settings
{
    internal static class AsrSettingsProvider
    {
        private const string ProviderPath = "Project/Sherpa-ONNX/ASR";
        private const string ProviderLabel = "ASR";

        private const string UxmlPath =
            "Packages/com.ponyudev.sherpa-onnx/" +
            "Editor/ASRInstall/UI/AsrSettings.uxml";

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

        private static AsrSettingsView _view;

        private static void Activate(
            string searchContext, VisualElement rootElement)
        {
            _view = new AsrSettingsView(uxmlPath: UxmlPath);
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
