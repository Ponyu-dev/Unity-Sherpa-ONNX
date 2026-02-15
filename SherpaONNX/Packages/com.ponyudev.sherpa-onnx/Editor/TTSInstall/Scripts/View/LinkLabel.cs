using UnityEngine;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.TtsInstall.View
{
    /// <summary>
    /// Clickable label that opens a URL in the browser.
    /// Changes color on hover.
    /// </summary>
    internal sealed class LinkLabel : Label
    {
        private static readonly Color NormalColor = new Color(0.45f, 0.65f, 1f);
        private static readonly Color HoverColor = new Color(0.6f, 0.8f, 1f);

        private readonly string _url;

        internal LinkLabel(string text, string url) : base(text)
        {
            _url = url;
            style.color = NormalColor;
            style.unityFontStyleAndWeight = FontStyle.Normal;
            RegisterCallback<PointerUpEvent>(HandleClick);
            RegisterCallback<PointerEnterEvent>(HandleEnter);
            RegisterCallback<PointerLeaveEvent>(HandleLeave);
        }

        private void HandleClick(PointerUpEvent evt)
        {
            Application.OpenURL(_url);
        }

        private void HandleEnter(PointerEnterEvent evt)
        {
            style.color = HoverColor;
        }

        private void HandleLeave(PointerLeaveEvent evt)
        {
            style.color = NormalColor;
        }
    }
}
