using UnityEditor;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.LibraryInstall
{
    internal static class SherpaOnnxSettingsProvider
    {
        private const string ProviderPath = "Project/Sherpa-ONNX";
        private const string ProviderLabel = "Sherpa-ONNX";
        private const string MainUxmlPath = "Packages/com.ponyudev.sherpa-onnx/Editor/LibraryInstall/UI/SherpaOnnxSettings.uxml";
        private const string TemplateUxmlPath = "Packages/com.ponyudev.sherpa-onnx/Editor/Common/UI/TemplateInstall.uxml";

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

        private static SherpaOnnxSettingsView _view;

        private static void Activate(string searchContext, VisualElement rootElement)
        {
            _view = new SherpaOnnxSettingsView(mainUxmlPath: MainUxmlPath, templateUxmlPath: TemplateUxmlPath);
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