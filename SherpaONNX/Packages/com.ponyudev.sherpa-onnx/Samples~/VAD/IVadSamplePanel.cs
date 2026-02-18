using System;
using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Common.Audio;
using PonyuDev.SherpaOnnx.Vad;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Contract for a VAD sample panel shown inside
    /// <see cref="VadSampleNavigator"/>.
    /// Each panel is a plain C# object — not a MonoBehaviour.
    /// </summary>
    public interface IVadSamplePanel
    {
        /// <summary>
        /// Bind UI elements and subscribe to events.
        /// Called every time the panel becomes visible.
        /// </summary>
        void Bind(
            VisualElement root,
            IVadService vadService,
            IAsrService asrService,
            VadAsrPipeline pipeline,
            MicrophoneSource microphone,
            Action onBack);

        /// <summary>
        /// Unsubscribe from events and release references.
        /// Called when the panel is about to be replaced.
        /// </summary>
        void Unbind();
    }
}
