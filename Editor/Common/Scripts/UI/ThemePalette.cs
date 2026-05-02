using UnityEditor;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.Common.UI
{
    /// <summary>
    /// Loads the shared theme stylesheet on a host element and tags it with
    /// `theme-dark` or `theme-light` so USS variables in Theme.uss resolve
    /// based on the current Editor skin.
    /// </summary>
    internal sealed class ThemePalette
    {
        private const string ThemeUssPath =
            "Packages/com.ponyudev.sherpa-onnx/Editor/Common/UI/Theme.uss";
        private const string ThemeDarkClass = "theme-dark";
        private const string ThemeLightClass = "theme-light";

        private VisualElement _host;
        private StyleSheet _styleSheet;

        internal void Apply(VisualElement host)
        {
            _host = host;

            _styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ThemeUssPath);
            if (_styleSheet != null)
                host.styleSheets.Add(_styleSheet);

            host.AddToClassList(EditorGUIUtility.isProSkin
                ? ThemeDarkClass : ThemeLightClass);
        }

        internal void Clear()
        {
            if (_host == null)
                return;

            _host.RemoveFromClassList(ThemeDarkClass);
            _host.RemoveFromClassList(ThemeLightClass);

            if (_styleSheet != null)
                _host.styleSheets.Remove(_styleSheet);

            _host = null;
            _styleSheet = null;
        }
    }
}
