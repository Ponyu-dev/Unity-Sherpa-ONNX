using System;
using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Asr.Online;
using PonyuDev.SherpaOnnx.Common.Audio;
using UnityEngine;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Contract for an ASR sample panel shown inside
    /// <see cref="AsrSampleNavigator"/>.
    /// Each panel is a plain C# object — not a MonoBehaviour.
    /// </summary>
    public interface IAsrSamplePanel
    {
        /// <summary>
        /// Bind UI elements and subscribe to events.
        /// Called every time the panel becomes visible.
        /// Each panel uses only the parameters it needs.
        /// </summary>
        void Bind(
            VisualElement root,
            IAsrService offlineService,
            IOnlineAsrService onlineService,
            MicrophoneSource microphone,
            AudioClip sampleClip,
            Action onBack);

        /// <summary>
        /// Unsubscribe from events and release references.
        /// Called when the panel is about to be replaced.
        /// </summary>
        void Unbind();
    }
}
