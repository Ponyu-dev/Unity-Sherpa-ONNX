using System;
using PonyuDev.SherpaOnnx.Common.Audio;
using PonyuDev.SherpaOnnx.Common.Audio.Config;
using PonyuDev.SherpaOnnx.Editor.Common.UI;
using PonyuDev.SherpaOnnx.Editor.Microphone.Settings;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.Microphone.View
{
    /// <summary>
    /// Top-level coordinator for Microphone settings UI.
    /// Loads UXML and wires explicit per-field change handlers,
    /// mirroring the Vad/Tts/Asr setting panels.
    /// </summary>
    internal sealed class MicrophoneSettingsView : IDisposable
    {
        private readonly string _uxmlPath;
        private readonly ThemePalette _themePalette = new();

        private IntegerField _sampleRateField;
        private IntegerField _clipLengthField;
        private FloatField _micStartTimeoutField;
        private EnumField _resamplingModeField;
        private FloatField _silenceThresholdField;
        private IntegerField _silenceFrameLimitField;
        private IntegerField _diagFrameCountField;

        internal MicrophoneSettingsView(string uxmlPath)
        {
            _uxmlPath = uxmlPath;
        }

        internal void Build(VisualElement hostRoot)
        {
            var uxmlAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(_uxmlPath);
            if (uxmlAsset == null)
            {
                Debug.LogError($"[SherpaOnnx] Microphone UXML not found: {_uxmlPath}");
                return;
            }

            _themePalette.Apply(hostRoot);
            uxmlAsset.CloneTree(hostRoot);
            LoadStyleSheets(hostRoot);

            BindFields(hostRoot, MicrophoneProjectSettings.instance.data);
        }

        public void Dispose()
        {
            _themePalette.Clear();

            _sampleRateField?.UnregisterValueChangedCallback(HandleSampleRateChanged);
            _clipLengthField?.UnregisterValueChangedCallback(HandleClipLengthChanged);
            _micStartTimeoutField?.UnregisterValueChangedCallback(HandleMicStartTimeoutChanged);
            _resamplingModeField?.UnregisterValueChangedCallback(HandleResamplingModeChanged);
            _silenceThresholdField?.UnregisterValueChangedCallback(HandleSilenceThresholdChanged);
            _silenceFrameLimitField?.UnregisterValueChangedCallback(HandleSilenceFrameLimitChanged);
            _diagFrameCountField?.UnregisterValueChangedCallback(HandleDiagFrameCountChanged);

            _sampleRateField = null;
            _clipLengthField = null;
            _micStartTimeoutField = null;
            _resamplingModeField = null;
            _silenceThresholdField = null;
            _silenceFrameLimitField = null;
            _diagFrameCountField = null;
        }

        // ── Bindings ──

        private void BindFields(VisualElement root, MicrophoneSettingsData data)
        {
            _sampleRateField = root.Q<IntegerField>("sampleRateField");
            if (_sampleRateField != null)
            {
                _sampleRateField.value = data.sampleRate;
                _sampleRateField.RegisterValueChangedCallback(HandleSampleRateChanged);
            }

            _clipLengthField = root.Q<IntegerField>("clipLengthField");
            if (_clipLengthField != null)
            {
                _clipLengthField.value = data.clipLengthSec;
                _clipLengthField.RegisterValueChangedCallback(HandleClipLengthChanged);
            }

            _micStartTimeoutField = root.Q<FloatField>("micStartTimeoutField");
            if (_micStartTimeoutField != null)
            {
                _micStartTimeoutField.value = data.micStartTimeoutSec;
                _micStartTimeoutField.RegisterValueChangedCallback(HandleMicStartTimeoutChanged);
            }

            _resamplingModeField = root.Q<EnumField>("resamplingModeField");
            if (_resamplingModeField != null)
            {
                _resamplingModeField.Init(data.resamplingMode);
                _resamplingModeField.value = data.resamplingMode;
                _resamplingModeField.RegisterValueChangedCallback(HandleResamplingModeChanged);
            }

            _silenceThresholdField = root.Q<FloatField>("silenceThresholdField");
            if (_silenceThresholdField != null)
            {
                _silenceThresholdField.value = data.silenceThreshold;
                _silenceThresholdField.RegisterValueChangedCallback(HandleSilenceThresholdChanged);
            }

            _silenceFrameLimitField = root.Q<IntegerField>("silenceFrameLimitField");
            if (_silenceFrameLimitField != null)
            {
                _silenceFrameLimitField.value = data.silenceFrameLimit;
                _silenceFrameLimitField.RegisterValueChangedCallback(HandleSilenceFrameLimitChanged);
            }

            _diagFrameCountField = root.Q<IntegerField>("diagFrameCountField");
            if (_diagFrameCountField != null)
            {
                _diagFrameCountField.value = data.diagFrameCount;
                _diagFrameCountField.RegisterValueChangedCallback(HandleDiagFrameCountChanged);
            }
        }

        // ── Persistence ──

        private static void HandleSampleRateChanged(ChangeEvent<int> evt)
        {
            var s = MicrophoneProjectSettings.instance;
            s.data.sampleRate = evt.newValue;
            s.SaveSettings();
        }

        private static void HandleClipLengthChanged(ChangeEvent<int> evt)
        {
            var s = MicrophoneProjectSettings.instance;
            s.data.clipLengthSec = evt.newValue;
            s.SaveSettings();
        }

        private static void HandleMicStartTimeoutChanged(ChangeEvent<float> evt)
        {
            var s = MicrophoneProjectSettings.instance;
            s.data.micStartTimeoutSec = evt.newValue;
            s.SaveSettings();
        }

        private static void HandleResamplingModeChanged(ChangeEvent<Enum> evt)
        {
            var s = MicrophoneProjectSettings.instance;
            s.data.resamplingMode = (ResamplingMode)evt.newValue;
            s.SaveSettings();
        }

        private static void HandleSilenceThresholdChanged(ChangeEvent<float> evt)
        {
            var s = MicrophoneProjectSettings.instance;
            s.data.silenceThreshold = evt.newValue;
            s.SaveSettings();
        }

        private static void HandleSilenceFrameLimitChanged(ChangeEvent<int> evt)
        {
            var s = MicrophoneProjectSettings.instance;
            s.data.silenceFrameLimit = evt.newValue;
            s.SaveSettings();
        }

        private static void HandleDiagFrameCountChanged(ChangeEvent<int> evt)
        {
            var s = MicrophoneProjectSettings.instance;
            s.data.diagFrameCount = evt.newValue;
            s.SaveSettings();
        }

        // ── Styles ──

        private void LoadStyleSheets(VisualElement root)
        {
            const string commonUssPath = "Packages/com.ponyudev.sherpa-onnx/Editor/Common/UI/ModelSettings.uss";
            var commonUss = AssetDatabase.LoadAssetAtPath<StyleSheet>(commonUssPath);
            if (commonUss != null)
                root.styleSheets.Add(commonUss);

            string ussPath = _uxmlPath.Replace(".uxml", ".uss");
            var ussAsset = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
            if (ussAsset != null)
                root.styleSheets.Add(ussAsset);
        }
    }
}
