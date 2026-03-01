using System;
using PonyuDev.SherpaOnnx.Common.Audio;
using PonyuDev.SherpaOnnx.Kws;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Contract for a KWS sample panel shown inside
    /// <see cref="KwsSampleNavigator"/>.
    /// Each panel is a plain C# object — not a MonoBehaviour.
    /// </summary>
    public interface IKwsSamplePanel
    {
        /// <summary>
        /// Bind UI elements and subscribe to events.
        /// Called every time the panel becomes visible.
        /// </summary>
        void Bind(
            VisualElement root,
            IKwsService kwsService,
            MicrophoneSource microphone,
            Action onBack);

        /// <summary>
        /// Unsubscribe from events and release references.
        /// Called when the panel is about to be replaced.
        /// </summary>
        void Unbind();
    }
}
