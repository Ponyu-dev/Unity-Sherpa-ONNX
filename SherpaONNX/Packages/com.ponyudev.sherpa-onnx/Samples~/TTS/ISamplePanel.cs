using System;
using PonyuDev.SherpaOnnx.Tts;
using UnityEngine;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Contract for a sample panel that can be shown inside the
    /// <see cref="SampleNavigator"/>.
    /// Each panel is a plain C# object — not a MonoBehaviour.
    /// </summary>
    public interface ISamplePanel
    {
        /// <summary>
        /// Bind UI elements and subscribe to events.
        /// Called every time the panel becomes visible.
        /// </summary>
        void Bind(
            VisualElement root,
            ITtsService service,
            AudioSource audio,
            Action onBack);

        /// <summary>
        /// Unsubscribe from events and release references.
        /// Called when the panel is about to be replaced.
        /// </summary>
        void Unbind();
    }
}
