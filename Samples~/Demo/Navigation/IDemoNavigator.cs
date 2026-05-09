namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Navigation surface exposed to <see cref="IDemoView"/>
    /// implementations. Views never hold a direct reference to the
    /// concrete <see cref="DemoNavigator"/> — they call
    /// <see cref="NavigateTo"/> / <see cref="Back"/> through this
    /// abstraction so they stay easy to host outside the demo scene
    /// (e.g. inside an Editor preview window) and free of
    /// MonoBehaviour coupling.
    ///
    /// View ids form a "/"-separated hierarchy:
    /// <c>"top"</c> → <c>"tts"</c> → <c>"tts/simple"</c>. The default
    /// <see cref="Back"/> implementation pops one segment off the
    /// active id, so the back stack is implicit and there is no
    /// per-view bookkeeping.
    /// </summary>
    public interface IDemoNavigator
    {
        /// <summary>Push the view registered under <paramref name="viewId"/>.</summary>
        void NavigateTo(string viewId);

        /// <summary>Return to the parent of the active view (top is a no-op).</summary>
        void Back();
    }
}
