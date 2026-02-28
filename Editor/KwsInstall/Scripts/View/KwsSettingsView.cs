using System;
using PonyuDev.SherpaOnnx.Editor.Common;
using PonyuDev.SherpaOnnx.Editor.Common.Import;
using PonyuDev.SherpaOnnx.Editor.Common.Presenters;
using PonyuDev.SherpaOnnx.Editor.KwsInstall.Import;
using PonyuDev.SherpaOnnx.Editor.KwsInstall.Presenters;
using PonyuDev.SherpaOnnx.Editor.KwsInstall.Settings;
using PonyuDev.SherpaOnnx.Kws.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.KwsInstall.View
{
    /// <summary>
    /// Top-level coordinator for KWS settings UI.
    /// Loads UXML, creates child presenters, manages links.
    /// </summary>
    internal sealed class KwsSettingsView : IDisposable
    {
        private const string KwsDocsUrl = "https://k2-fsa.github.io/sherpa/onnx/kws/index.html#pretrained-models";

        private readonly string _uxmlPath;
        private Toggle _kwsEnabledToggle;

        private ActiveProfilePresenter<KwsProfile> _activeProfilePresenter;
        private ProfileListPresenter<KwsProfile> _listPresenter;
        private KwsProfileDetailPresenter _detailPresenter;
        private KwsImportPresenter _importPresenter;
        private Button _importFromUrlButton;
        private VisualElement _importSection;

        internal KwsSettingsView(string uxmlPath)
        {
            _uxmlPath = uxmlPath;
        }

        internal void Build(VisualElement hostRoot)
        {
            var uxmlAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(_uxmlPath);
            if (uxmlAsset == null)
            {
                Debug.LogError($"[SherpaOnnx] KWS UXML not found: {_uxmlPath}");
                return;
            }

            uxmlAsset.CloneTree(hostRoot);
            LoadStyleSheets(hostRoot);

            KwsProjectSettings settings = KwsProjectSettings.instance;

            BindEnabledToggle(hostRoot, settings);
            BindLinks(hostRoot);
            BuildProfilePresenters(hostRoot, settings);
        }

        public void Dispose()
        {
            _kwsEnabledToggle?.UnregisterValueChangedCallback(HandleKwsEnabledChanged);
            _kwsEnabledToggle = null;

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

        private void BindEnabledToggle(VisualElement root, KwsProjectSettings settings)
        {
            _kwsEnabledToggle = root.Q<Toggle>("kwsEnabledToggle");
            if (_kwsEnabledToggle == null) return;

            _kwsEnabledToggle.value = settings.kwsEnabled;
            _kwsEnabledToggle.RegisterValueChangedCallback(HandleKwsEnabledChanged);
        }

        private static void HandleKwsEnabledChanged(ChangeEvent<bool> evt)
        {
            var s = KwsProjectSettings.instance;
            s.kwsEnabled = evt.newValue;
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

        // ── Links ──

        private static void BindLinks(VisualElement root)
        {
            var label = root.Q<Label>("linkModels");
            label?.RegisterCallback<PointerUpEvent, string>(HandleLinkClicked, KwsDocsUrl);
        }

        private static void HandleLinkClicked(PointerUpEvent evt, string url)
        {
            Application.OpenURL(url);
        }

        // ── Profiles ──

        private void BuildProfilePresenters(VisualElement root, KwsProjectSettings settings)
        {
            var activeSection = root.Q<VisualElement>("activeProfileSection");
            _activeProfilePresenter = new ActiveProfilePresenter<KwsProfile>(settings.data, settings, ModelPaths.GetKwsModelDir, ProfileFieldValidator.HasMissingFields);
            _activeProfilePresenter.Build(activeSection);

            _importSection = root.Q<VisualElement>("importSection");
            _importPresenter = new KwsImportPresenter(settings, HandleImportCompleted);
            _importPresenter.Build(_importSection);

            _importFromUrlButton = root.Q<Button>("importFromUrlButton");
            _importFromUrlButton.clicked += HandleImportFromUrlClicked;

            var listView = root.Q<ListView>("profilesListView");
            var addButton = root.Q<Button>("addProfileButton");
            var removeButton = root.Q<Button>("removeProfileButton");
            var detailContent = root.Q<VisualElement>("detailContent");

            _listPresenter = new ProfileListPresenter<KwsProfile>(settings.data, settings, ModelPaths.GetKwsModelDir, "model-list-item", ProfileFieldValidator.HasMissingFields);
            _detailPresenter = new KwsProfileDetailPresenter(detailContent, settings);
            _detailPresenter.SetListPresenter(_listPresenter);

            _listPresenter.SelectionChanged += HandleSelectionChanged;
            _listPresenter.Build(listView, addButton, removeButton);
        }

        private void HandleImportFromUrlClicked()
        {
            _importSection?.ToggleInClassList("hidden");
        }

        private void HandleImportCompleted()
        {
            _listPresenter?.RefreshList();
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
