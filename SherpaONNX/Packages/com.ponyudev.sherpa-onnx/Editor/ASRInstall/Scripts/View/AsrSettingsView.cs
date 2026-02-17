using System;
using PonyuDev.SherpaOnnx.Editor.AsrInstall.Import;
using PonyuDev.SherpaOnnx.Editor.AsrInstall.Presenters.Offline;
using PonyuDev.SherpaOnnx.Editor.AsrInstall.Presenters.Online;
using PonyuDev.SherpaOnnx.Editor.AsrInstall.Settings;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.AsrInstall.View
{
    /// <summary>
    /// Top-level coordinator for ASR settings UI.
    /// Two tabs (Offline / Online) switch between containers.
    /// </summary>
    internal sealed class AsrSettingsView : IDisposable
    {
        private const string GitHubModelsUrl =
            "https://github.com/k2-fsa/sherpa-onnx/releases/tag/asr-models";
        private const string ActiveTabClass = "asr-tab-active";

        private readonly string _uxmlPath;
        private Button _offlineTabBtn;
        private Button _onlineTabBtn;
        private VisualElement _offlineContainer;
        private VisualElement _onlineContainer;
        private AsrActiveProfilePresenter _offlineActivePresenter;
        private AsrProfileListPresenter _offlineListPresenter;
        private AsrProfileDetailPresenter _offlineDetailPresenter;
        private AsrImportPresenter _offlineImportPresenter;
        private Button _offlineImportFromUrlButton;
        private VisualElement _offlineImportSection;
        private OnlineAsrActiveProfilePresenter _onlineActivePresenter;
        private OnlineAsrProfileListPresenter _onlineListPresenter;
        private OnlineAsrProfileDetailPresenter _onlineDetailPresenter;
        private OnlineAsrImportPresenter _onlineImportPresenter;
        private Button _onlineImportFromUrlButton;
        private VisualElement _onlineImportSection;

        internal AsrSettingsView(string uxmlPath)
        {
            _uxmlPath = uxmlPath;
        }

        internal void Build(VisualElement hostRoot)
        {
            var uxmlAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                _uxmlPath);
            if (uxmlAsset == null)
            {
                Debug.LogError(
                    $"[SherpaOnnx] ASR UXML not found: {_uxmlPath}");
                return;
            }

            uxmlAsset.CloneTree(hostRoot);

            string ussPath = _uxmlPath.Replace(".uxml", ".uss");
            var ussAsset = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
            if (ussAsset != null)
                hostRoot.styleSheets.Add(ussAsset);

            AsrProjectSettings settings = AsrProjectSettings.instance;

            BindLinks(hostRoot);
            BindTabs(hostRoot);
            BuildOfflinePresenters(hostRoot, settings);
            BuildOnlinePresenters(hostRoot, settings);
        }

        public void Dispose()
        {
            DisposeOffline();
            DisposeOnline();
            if (_offlineTabBtn != null) _offlineTabBtn.clicked -= HandleOfflineTabClicked;
            if (_onlineTabBtn != null) _onlineTabBtn.clicked -= HandleOnlineTabClicked;
            _offlineTabBtn = null;
            _onlineTabBtn = null;
            _offlineContainer = null;
            _onlineContainer = null;
        }

        private static void BindLinks(VisualElement root)
        {
            var label = root.Q<Label>("linkModels");
            if (label != null)
                label.RegisterCallback<PointerUpEvent, string>(
                    HandleLinkClicked, GitHubModelsUrl);
        }

        private static void HandleLinkClicked(
            PointerUpEvent evt, string url)
        {
            Application.OpenURL(url);
        }

        private void BindTabs(VisualElement root)
        {
            _offlineTabBtn = root.Q<Button>("offlineTabBtn");
            _onlineTabBtn = root.Q<Button>("onlineTabBtn");
            _offlineContainer = root.Q<VisualElement>("offlineContainer");
            _onlineContainer = root.Q<VisualElement>("onlineContainer");

            _offlineTabBtn.clicked += HandleOfflineTabClicked;
            _onlineTabBtn.clicked += HandleOnlineTabClicked;
        }

        private void HandleOfflineTabClicked()
        {
            _offlineTabBtn.AddToClassList(ActiveTabClass);
            _onlineTabBtn.RemoveFromClassList(ActiveTabClass);
            _offlineContainer.RemoveFromClassList("hidden");
            _onlineContainer.AddToClassList("hidden");
        }

        private void HandleOnlineTabClicked()
        {
            _onlineTabBtn.AddToClassList(ActiveTabClass);
            _offlineTabBtn.RemoveFromClassList(ActiveTabClass);
            _onlineContainer.RemoveFromClassList("hidden");
            _offlineContainer.AddToClassList("hidden");
        }

        private void BuildOfflinePresenters(
            VisualElement root, AsrProjectSettings settings)
        {
            var activeSection = root.Q<VisualElement>(
                "offlineActiveProfileSection");
            _offlineActivePresenter = new AsrActiveProfilePresenter(settings);
            _offlineActivePresenter.Build(activeSection);

            _offlineImportSection = root.Q<VisualElement>(
                "offlineImportSection");
            _offlineImportPresenter = new AsrImportPresenter(
                settings, HandleOfflineImportCompleted);
            _offlineImportPresenter.Build(_offlineImportSection);

            _offlineImportFromUrlButton = root.Q<Button>(
                "offlineImportFromUrlButton");
            _offlineImportFromUrlButton.clicked +=
                HandleOfflineImportFromUrlClicked;

            var listView = root.Q<ListView>("offlineProfilesListView");
            var addBtn = root.Q<Button>("offlineAddProfileButton");
            var removeBtn = root.Q<Button>("offlineRemoveProfileButton");
            var detail = root.Q<VisualElement>("offlineDetailContent");

            _offlineListPresenter = new AsrProfileListPresenter(settings);
            _offlineDetailPresenter = new AsrProfileDetailPresenter(
                detail, settings);
            _offlineDetailPresenter.SetListPresenter(_offlineListPresenter);

            _offlineListPresenter.SelectionChanged +=
                HandleOfflineSelectionChanged;
            _offlineListPresenter.Build(listView, addBtn, removeBtn);
        }

        private void HandleOfflineImportFromUrlClicked() =>
            _offlineImportSection?.ToggleInClassList("hidden");

        private void HandleOfflineImportCompleted()
        {
            _offlineListPresenter?.RefreshList();
            _offlineActivePresenter?.Refresh();
        }

        private void HandleOfflineSelectionChanged(int index)
        {
            if (index >= 0) _offlineDetailPresenter.ShowProfile(index);
            else _offlineDetailPresenter.Clear();
            _offlineActivePresenter?.Refresh();
        }

        private void BuildOnlinePresenters(
            VisualElement root, AsrProjectSettings settings)
        {
            var activeSection = root.Q<VisualElement>(
                "onlineActiveProfileSection");
            _onlineActivePresenter =
                new OnlineAsrActiveProfilePresenter(settings);
            _onlineActivePresenter.Build(activeSection);

            _onlineImportSection = root.Q<VisualElement>(
                "onlineImportSection");
            _onlineImportPresenter = new OnlineAsrImportPresenter(
                settings, HandleOnlineImportCompleted);
            _onlineImportPresenter.Build(_onlineImportSection);

            _onlineImportFromUrlButton = root.Q<Button>(
                "onlineImportFromUrlButton");
            _onlineImportFromUrlButton.clicked +=
                HandleOnlineImportFromUrlClicked;

            var listView = root.Q<ListView>("onlineProfilesListView");
            var addBtn = root.Q<Button>("onlineAddProfileButton");
            var removeBtn = root.Q<Button>("onlineRemoveProfileButton");
            var detail = root.Q<VisualElement>("onlineDetailContent");

            _onlineListPresenter =
                new OnlineAsrProfileListPresenter(settings);
            _onlineDetailPresenter =
                new OnlineAsrProfileDetailPresenter(detail, settings);
            _onlineDetailPresenter.SetListPresenter(_onlineListPresenter);

            _onlineListPresenter.SelectionChanged +=
                HandleOnlineSelectionChanged;
            _onlineListPresenter.Build(listView, addBtn, removeBtn);
        }

        private void HandleOnlineImportFromUrlClicked() =>
            _onlineImportSection?.ToggleInClassList("hidden");

        private void HandleOnlineImportCompleted()
        {
            _onlineListPresenter?.RefreshList();
            _onlineActivePresenter?.Refresh();
        }

        private void HandleOnlineSelectionChanged(int index)
        {
            if (index >= 0) _onlineDetailPresenter.ShowProfile(index);
            else _onlineDetailPresenter.Clear();
            _onlineActivePresenter?.Refresh();
        }

        private void DisposeOffline()
        {
            _offlineActivePresenter?.Dispose();
            _offlineImportPresenter?.Dispose();
            if (_offlineImportFromUrlButton != null)
                _offlineImportFromUrlButton.clicked -= HandleOfflineImportFromUrlClicked;
            if (_offlineListPresenter != null)
                _offlineListPresenter.SelectionChanged -= HandleOfflineSelectionChanged;
            _offlineListPresenter?.Dispose();
            _offlineDetailPresenter?.Dispose();
            _offlineActivePresenter = null;
            _offlineImportPresenter = null;
            _offlineImportFromUrlButton = null;
            _offlineImportSection = null;
            _offlineListPresenter = null;
            _offlineDetailPresenter = null;
        }

        private void DisposeOnline()
        {
            _onlineActivePresenter?.Dispose();
            _onlineImportPresenter?.Dispose();
            if (_onlineImportFromUrlButton != null)
                _onlineImportFromUrlButton.clicked -= HandleOnlineImportFromUrlClicked;
            if (_onlineListPresenter != null)
                _onlineListPresenter.SelectionChanged -= HandleOnlineSelectionChanged;
            _onlineListPresenter?.Dispose();
            _onlineDetailPresenter?.Dispose();
            _onlineActivePresenter = null;
            _onlineImportPresenter = null;
            _onlineImportFromUrlButton = null;
            _onlineImportSection = null;
            _onlineListPresenter = null;
            _onlineDetailPresenter = null;
        }
    }
}
