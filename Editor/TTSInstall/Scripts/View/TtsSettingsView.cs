using System;
using PonyuDev.SherpaOnnx.Editor.Common.Presenters;
using PonyuDev.SherpaOnnx.Editor.TtsInstall.Import;
using PonyuDev.SherpaOnnx.Editor.TtsInstall.Presenters;
using PonyuDev.SherpaOnnx.Editor.TtsInstall.Settings;
using PonyuDev.SherpaOnnx.Tts.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.TtsInstall.View
{
    /// <summary>
    /// Top-level coordinator for TTS settings UI.
    /// Loads UXML, creates child presenters, manages cache fields and links.
    /// </summary>
    internal sealed class TtsSettingsView : IDisposable
    {
        private const string HuggingFaceUrl =
            "https://huggingface.co/spaces/k2-fsa/text-to-speech";
        private const string GitHubModelsUrl =
            "https://github.com/k2-fsa/sherpa-onnx/releases/tag/tts-models";

        private readonly string _uxmlPath;
        private Toggle _ttsEnabledToggle;

        private ActiveProfilePresenter<TtsProfile> _activeProfilePresenter;
        private ProfileListPresenter<TtsProfile> _listPresenter;
        private TtsProfileDetailPresenter _detailPresenter;
        private TtsImportPresenter _importPresenter;
        private Button _importFromUrlButton;
        private VisualElement _importSection;

        internal TtsSettingsView(string uxmlPath)
        {
            _uxmlPath = uxmlPath;
        }

        internal void Build(VisualElement hostRoot)
        {
            var uxmlAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(_uxmlPath);
            if (uxmlAsset == null)
            {
                Debug.LogError($"[SherpaOnnx] TTS UXML not found: {_uxmlPath}");
                return;
            }

            uxmlAsset.CloneTree(hostRoot);

            LoadStyleSheets(hostRoot);

            TtsProjectSettings settings = TtsProjectSettings.instance;

            BindEnabledToggle(hostRoot, settings);
            BindLinks(hostRoot);
            BuildCacheSection(hostRoot, settings);
            BuildProfilePresenters(hostRoot, settings);
        }

        public void Dispose()
        {
            _ttsEnabledToggle?.UnregisterValueChangedCallback(
                HandleTtsEnabledChanged);
            _ttsEnabledToggle = null;

            _activeProfilePresenter?.Dispose();
            _activeProfilePresenter = null;

            _importPresenter?.Dispose();
            _importPresenter = null;

            if (_importFromUrlButton != null)
                _importFromUrlButton.clicked -= HandleImportFromUrlClicked;
            _importFromUrlButton = null;
            _importSection = null;

            if (_listPresenter != null)
                _listPresenter.SelectionChanged -= HandleSelectionChanged;

            _listPresenter?.Dispose();
            _listPresenter = null;

            _detailPresenter?.Dispose();
            _detailPresenter = null;
        }

        // ── Module Toggle ──

        private void BindEnabledToggle(
            VisualElement root, TtsProjectSettings settings)
        {
            _ttsEnabledToggle = root.Q<Toggle>("ttsEnabledToggle");
            if (_ttsEnabledToggle == null) return;

            _ttsEnabledToggle.value = settings.ttsEnabled;
            _ttsEnabledToggle.RegisterValueChangedCallback(
                HandleTtsEnabledChanged);
        }

        private static void HandleTtsEnabledChanged(
            ChangeEvent<bool> evt)
        {
            var s = TtsProjectSettings.instance;
            s.ttsEnabled = evt.newValue;
            s.SaveSettings();
        }

        // ── Styles ──

        private void LoadStyleSheets(VisualElement root)
        {
            const string commonUssPath =
                "Packages/com.ponyudev.sherpa-onnx/Editor/Common/UI/ModelSettings.uss";
            var commonUss =
                AssetDatabase.LoadAssetAtPath<StyleSheet>(commonUssPath);
            if (commonUss != null)
                root.styleSheets.Add(commonUss);

            string ussPath = _uxmlPath.Replace(".uxml", ".uss");
            var ussAsset =
                AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
            if (ussAsset != null)
                root.styleSheets.Add(ussAsset);
        }

        // ── Links ──

        private static void BindLinks(VisualElement root)
        {
            RegisterLink(root, "linkModels", GitHubModelsUrl);
            RegisterLink(root, "linkTryVoices", HuggingFaceUrl);
        }

        private static void RegisterLink(VisualElement root, string name, string url)
        {
            var label = root.Q<Label>(name);
            if (label == null) return;

            label.RegisterCallback<PointerUpEvent, string>(HandleLinkClicked, url);
        }

        private static void HandleLinkClicked(PointerUpEvent evt, string url)
        {
            Application.OpenURL(url);
        }

        // ── Cache ──

        private static void BuildCacheSection(
            VisualElement root, TtsProjectSettings settings)
        {
            var header = root.Q<VisualElement>("tts-header");
            if (header?.parent == null)
                return;

            TtsCacheSettings cache = settings.data.cache;

            var foldout = new Foldout { text = "Cache Settings" };
            foldout.AddToClassList("model-foldout");

            foldout.Add(new CacheGroupElement(
                "OfflineTts Engine Pool",
                "Pool of native OfflineTts instances.\n"
                + "Allows generating multiple phrases concurrently on background threads.",
                cache.offlineTtsEnabled, cache.offlineTtsPoolSize,
                cache, settings, CacheField.OfflineTts));

            foldout.Add(new CacheGroupElement(
                "Result Cache (float[])",
                "LRU cache of generated audio data (float[] clones).\n"
                + "Avoids re-synthesis when the same phrase is requested again.",
                cache.resultCacheEnabled, cache.resultCacheSize,
                cache, settings, CacheField.ResultCache));

            foldout.Add(new CacheGroupElement(
                "AudioClip Pool",
                "Pool of reusable AudioClip objects.\n"
                + "Avoids allocating new clips for every playback.",
                cache.audioClipEnabled, cache.audioClipPoolSize,
                cache, settings, CacheField.AudioClip));

            foldout.Add(new CacheGroupElement(
                "AudioSource Pool",
                "Pool of AudioSource GameObjects for parallel playback.\n"
                + "Allows multiple phrases to play simultaneously.",
                cache.audioSourceEnabled, cache.audioSourcePoolSize,
                cache, settings, CacheField.AudioSource));

            VisualElement container = header.parent;
            int idx = container.IndexOf(header) + 2;
            container.Insert(idx, foldout);
        }

        // ── Profiles ──

        private void BuildProfilePresenters(
            VisualElement root, TtsProjectSettings settings)
        {
            var activeSection = root.Q<VisualElement>("activeProfileSection");
            _activeProfilePresenter =
                new ActiveProfilePresenter<TtsProfile>(settings.data, settings, TtsModelPaths.GetModelDir);
            _activeProfilePresenter.Build(activeSection);

            _importSection = root.Q<VisualElement>("importSection");
            _importPresenter = new TtsImportPresenter(settings, HandleImportCompleted);
            _importPresenter.Build(_importSection);

            _importFromUrlButton = root.Q<Button>("importFromUrlButton");
            _importFromUrlButton.clicked += HandleImportFromUrlClicked;

            var listView = root.Q<ListView>("profilesListView");
            var addButton = root.Q<Button>("addProfileButton");
            var removeButton = root.Q<Button>("removeProfileButton");
            var detailContent = root.Q<VisualElement>("detailContent");

            _listPresenter = new ProfileListPresenter<TtsProfile>(
                settings.data, settings,
                TtsModelPaths.GetModelDir, "model-list-item");
            _detailPresenter = new TtsProfileDetailPresenter(detailContent, settings);
            _detailPresenter.SetListPresenter(_listPresenter);

            _listPresenter.SelectionChanged += HandleSelectionChanged;
            _listPresenter.Build(listView, addButton, removeButton);
        }

        private void HandleImportFromUrlClicked()
        {
            if (_importSection == null) return;

            _importSection.ToggleInClassList("hidden");
        }

        private void HandleImportCompleted()
        {
            _listPresenter.RefreshList();
            _activeProfilePresenter?.Refresh();
        }

        private void HandleSelectionChanged(int index)
        {
            if (index >= 0)
                _detailPresenter.ShowProfile(index);
            else
                _detailPresenter.Clear();

            _activeProfilePresenter?.Refresh();
        }
    }
}
