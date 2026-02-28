using PonyuDev.SherpaOnnx.Editor.KwsInstall.View;
using UnityEditor;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.KwsInstall.Settings
{
    internal static class KwsSettingsProvider
    {
        private const string ProviderPath = "Project/Sherpa-ONNX/KWS";
        private const string ProviderLabel = "KWS";

        private const string UxmlPath = "Packages/com.ponyudev.sherpa-onnx/Editor/KwsInstall/UI/KwsSettings.uxml";

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

        private static KwsSettingsView _view;

        private static void Activate(string searchContext, VisualElement rootElement)
        {
            _view = new KwsSettingsView(uxmlPath: UxmlPath);
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
