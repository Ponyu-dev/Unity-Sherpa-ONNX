using System;
using PonyuDev.SherpaOnnx.Editor.Common.Presenters;
using PonyuDev.SherpaOnnx.Editor.VadInstall.Import;
using PonyuDev.SherpaOnnx.Editor.VadInstall.Presenters;
using PonyuDev.SherpaOnnx.Editor.VadInstall.Settings;
using PonyuDev.SherpaOnnx.Vad.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.VadInstall.View
{
    /// <summary>
    /// Top-level coordinator for VAD settings UI.
    /// Loads UXML, creates child presenters, manages links.
    /// </summary>
    internal sealed class VadSettingsView : IDisposable
    {
        private const string VadDocsUrl = "https://k2-fsa.github.io/sherpa/onnx/vad/index.html";

        private readonly string _uxmlPath;
        private Toggle _vadEnabledToggle;

        private ActiveProfilePresenter<VadProfile> _activeProfilePresenter;
        private ProfileListPresenter<VadProfile> _listPresenter;
        private VadProfileDetailPresenter _detailPresenter;
        private VadImportPresenter _importPresenter;
        private Button _importFromUrlButton;
        private VisualElement _importSection;

        internal VadSettingsView(string uxmlPath)
        {
            _uxmlPath = uxmlPath;
        }

        internal void Build(VisualElement hostRoot)
        {
            var uxmlAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(_uxmlPath);
            if (uxmlAsset == null)
            {
                Debug.LogError($"[SherpaOnnx] VAD UXML not found: {_uxmlPath}");
                return;
            }

            uxmlAsset.CloneTree(hostRoot);
            LoadStyleSheets(hostRoot);

            VadProjectSettings settings = VadProjectSettings.instance;

            BindEnabledToggle(hostRoot, settings);
            BindLinks(hostRoot);
            BuildProfilePresenters(hostRoot, settings);
        }

        public void Dispose()
        {
            _vadEnabledToggle?.UnregisterValueChangedCallback(HandleVadEnabledChanged);
            _vadEnabledToggle = null;

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
            VisualElement root, VadProjectSettings settings)
        {
            _vadEnabledToggle = root.Q<Toggle>("vadEnabledToggle");
            if (_vadEnabledToggle == null) return;

            _vadEnabledToggle.value = settings.vadEnabled;
            _vadEnabledToggle.RegisterValueChangedCallback(HandleVadEnabledChanged);
        }

        private static void HandleVadEnabledChanged(ChangeEvent<bool> evt)
        {
            var s = VadProjectSettings.instance;
            s.vadEnabled = evt.newValue;
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
            label?.RegisterCallback<PointerUpEvent, string>(HandleLinkClicked, VadDocsUrl);
        }

        private static void HandleLinkClicked(
            PointerUpEvent evt, string url)
        {
            Application.OpenURL(url);
        }

        // ── Profiles ──

        private void BuildProfilePresenters(
            VisualElement root, VadProjectSettings settings)
        {
            var activeSection = root.Q<VisualElement>("activeProfileSection");
            _activeProfilePresenter = new ActiveProfilePresenter<VadProfile>(settings.data, settings);
            _activeProfilePresenter.Build(activeSection);

            _importSection = root.Q<VisualElement>("importSection");
            _importPresenter = new VadImportPresenter(settings, HandleImportCompleted);
            _importPresenter.Build(_importSection);

            _importFromUrlButton = root.Q<Button>("importFromUrlButton");
            _importFromUrlButton.clicked += HandleImportFromUrlClicked;

            var listView = root.Q<ListView>("profilesListView");
            var addButton = root.Q<Button>("addProfileButton");
            var removeButton = root.Q<Button>("removeProfileButton");
            var detailContent = root.Q<VisualElement>("detailContent");

            _listPresenter = new ProfileListPresenter<VadProfile>(settings.data, settings, VadModelPaths.GetModelDir, "model-list-item");
            _detailPresenter = new VadProfileDetailPresenter(detailContent, settings);
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
