using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Contract for any UI screen the <see cref="DemoNavigator"/>
    /// can show — the top menu, a sub-menu, or a content panel. Each
    /// view is a plain C# object (no MonoBehaviour); the navigator
    /// builds it once, calls <see cref="Bind"/> when the view becomes
    /// visible, and <see cref="Unbind"/> right before swapping it out.
    /// Bind / Unbind are paired and may be called multiple times
    /// across the view's lifetime as the user navigates back and
    /// forth.
    /// </summary>
    public interface IDemoView
    {
        /// <summary>
        /// Wire up UI elements, subscribe to service / bus events,
        /// and call <see cref="IDemoNavigator.NavigateTo"/> /
        /// <see cref="IDemoNavigator.Back"/> from button handlers.
        /// </summary>
        void Bind(
            VisualElement root,
            DemoServices services,
            IDemoNavigator nav);

        /// <summary>
        /// Unsubscribe and release every reference acquired during
        /// <see cref="Bind"/>. Called before the navigator swaps the
        /// active <c>VisualTreeAsset</c>.
        /// </summary>
        void Unbind();
    }
}
