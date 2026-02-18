using PonyuDev.SherpaOnnx.Editor.VadInstall.View;
using UnityEditor;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.VadInstall.Settings
{
    internal static class VadSettingsProvider
    {
        private const string ProviderPath = "Project/Sherpa-ONNX/VAD";
        private const string ProviderLabel = "VAD";

        private const string UxmlPath = "Packages/com.ponyudev.sherpa-onnx/Editor/VadInstall/UI/VadSettings.uxml";

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

        private static VadSettingsView _view;

        private static void Activate(string searchContext, VisualElement rootElement)
        {
            _view = new VadSettingsView(uxmlPath: UxmlPath);
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
