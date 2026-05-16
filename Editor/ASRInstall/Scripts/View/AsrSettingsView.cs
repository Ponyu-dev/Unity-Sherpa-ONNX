using System;
using PonyuDev.SherpaOnnx.Asr.Offline.Data;
using PonyuDev.SherpaOnnx.Asr.Online.Data;
using PonyuDev.SherpaOnnx.Editor.AsrInstall.Import;
using PonyuDev.SherpaOnnx.Editor.AsrInstall.Presenters.Offline;
using PonyuDev.SherpaOnnx.Editor.AsrInstall.Presenters.Online;
using PonyuDev.SherpaOnnx.Editor.AsrInstall.Settings;
using PonyuDev.SherpaOnnx.Editor.Common;
using PonyuDev.SherpaOnnx.Editor.Common.Import;
using PonyuDev.SherpaOnnx.Editor.Common.Presenters;
using PonyuDev.SherpaOnnx.Editor.Common.UI;
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
        private const string GitHubModelsUrl = "https://github.com/k2-fsa/sherpa-onnx/releases/tag/asr-models";
        private const string ActiveTabClass = "asr-tab-active";

        private readonly string _uxmlPath;
        private Toggle _asrEnabledToggle;
        private Toggle _offlineKeepOnlyActiveProfileToggle;
        private Toggle _onlineKeepOnlyActiveProfileToggle;
        private Toggle _offlineBuildOnlyActiveProfileToggle;
        private Toggle _onlineBuildOnlyActiveProfileToggle;
        private Button _offlineTabBtn;
        private Button _onlineTabBtn;
        private VisualElement _offlineContainer;
        private VisualElement _onlineContainer;
        private ActiveProfilePresenter<AsrProfile> _offlineActivePresenter;
        private ProfileListPresenter<AsrProfile> _offlineListPresenter;
        private AsrProfileDetailPresenter _offlineDetailPresenter;
        private AsrImportPresenter _offlineImportPresenter;
        private Button _offlineImportFromUrlButton;
        private VisualElement _offlineImportSection;
        private ActiveProfilePresenter<OnlineAsrProfile> _onlineActivePresenter;
        private ProfileListPresenter<OnlineAsrProfile> _onlineListPresenter;
        private OnlineAsrProfileDetailPresenter _onlineDetailPresenter;
        private OnlineAsrImportPresenter _onlineImportPresenter;
        private Button _onlineImportFromUrlButton;
        private VisualElement _onlineImportSection;
        private readonly ThemePalette _themePalette = new();

        internal AsrSettingsView(string uxmlPath)
        {
            _uxmlPath = uxmlPath;
        }

        internal void Build(VisualElement hostRoot)
        {
            var uxmlAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(_uxmlPath);
            if (uxmlAsset == null)
            {
                Debug.LogError($"[SherpaOnnx] ASR UXML not found: {_uxmlPath}");
                return;
            }

            _themePalette.Apply(hostRoot);
            uxmlAsset.CloneTree(hostRoot);
            LoadStyleSheets(hostRoot);

            AsrProjectSettings settings = AsrProjectSettings.instance;

            BindEnabledToggle(hostRoot, settings);
            BindLinks(hostRoot);
            BindTabs(hostRoot);
            BuildOfflinePresenters(settings);
            BuildOnlinePresenters(settings);
        }

        public void Dispose()
        {
            _themePalette.Clear();

            DisposeOffline();
            DisposeOnline();
            _asrEnabledToggle?.UnregisterValueChangedCallback(HandleAsrEnabledChanged);
            _asrEnabledToggle = null;
            if (_offlineTabBtn != null) _offlineTabBtn.clicked -= HandleOfflineTabClicked;
            if (_onlineTabBtn != null) _onlineTabBtn.clicked -= HandleOnlineTabClicked;
            _offlineTabBtn = null;
            _onlineTabBtn = null;
            _offlineContainer = null;
            _onlineContainer = null;
        }

        private void BindEnabledToggle(VisualElement root, AsrProjectSettings settings)
        {
            _asrEnabledToggle = root.Q<Toggle>("asrEnabledToggle");
            if (_asrEnabledToggle == null)
                return;
            _asrEnabledToggle.value = settings.asrEnabled;
            _asrEnabledToggle.RegisterValueChangedCallback(HandleAsrEnabledChanged);
        }

        private static void HandleAsrEnabledChanged(ChangeEvent<bool> evt)
        {
            var s = AsrProjectSettings.instance;
            s.asrEnabled = evt.newValue;
            s.SaveSettings();
        }

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

        private static void BindLinks(VisualElement root)
        {
            var label = root.Q<Label>("linkModels");
            label?.RegisterCallback<PointerUpEvent, string>(HandleLinkClicked, GitHubModelsUrl);
        }

        private static void HandleLinkClicked(PointerUpEvent evt, string url)
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

        private void BuildOfflineAutoDeleteToggle(AsrProjectSettings settings)
        {
            var foldout = new Foldout { text = "Disk Usage" };
            foldout.AddToClassList("model-foldout");

            _offlineKeepOnlyActiveProfileToggle = new Toggle(KeepOnlyActiveProfileToggle.Label)
            {
                tooltip = KeepOnlyActiveProfileToggle.Tooltip,
                value = settings.offlineData.keepOnlyActiveProfile,
            };
            _offlineKeepOnlyActiveProfileToggle.RegisterValueChangedCallback(
                HandleOfflineKeepOnlyActiveProfileChanged);

            foldout.Add(_offlineKeepOnlyActiveProfileToggle);

            _offlineBuildOnlyActiveProfileToggle = new Toggle(OnlyActiveProfileInBuildToggle.Label)
            {
                tooltip = OnlyActiveProfileInBuildToggle.Tooltip,
                value = settings.offlineData.buildOnlyActiveProfile,
            };
            _offlineBuildOnlyActiveProfileToggle.RegisterValueChangedCallback(
                HandleOfflineBuildOnlyActiveProfileChanged);

            foldout.Add(_offlineBuildOnlyActiveProfileToggle);
            _offlineContainer.Insert(0, foldout);
        }

        private void BuildOnlineAutoDeleteToggle(AsrProjectSettings settings)
        {
            var foldout = new Foldout { text = "Disk Usage" };
            foldout.AddToClassList("model-foldout");

            _onlineKeepOnlyActiveProfileToggle = new Toggle(KeepOnlyActiveProfileToggle.Label)
            {
                tooltip = KeepOnlyActiveProfileToggle.Tooltip,
                value = settings.onlineData.keepOnlyActiveProfile,
            };
            _onlineKeepOnlyActiveProfileToggle.RegisterValueChangedCallback(
                HandleOnlineKeepOnlyActiveProfileChanged);

            foldout.Add(_onlineKeepOnlyActiveProfileToggle);

            _onlineBuildOnlyActiveProfileToggle = new Toggle(OnlyActiveProfileInBuildToggle.Label)
            {
                tooltip = OnlyActiveProfileInBuildToggle.Tooltip,
                value = settings.onlineData.buildOnlyActiveProfile,
            };
            _onlineBuildOnlyActiveProfileToggle.RegisterValueChangedCallback(
                HandleOnlineBuildOnlyActiveProfileChanged);

            foldout.Add(_onlineBuildOnlyActiveProfileToggle);
            _onlineContainer.Insert(0, foldout);
        }

        private static void HandleOfflineKeepOnlyActiveProfileChanged(ChangeEvent<bool> evt)
        {
            var s = AsrProjectSettings.instance;
            s.offlineData.keepOnlyActiveProfile = evt.newValue;
            s.SaveSettings();
        }

        private static void HandleOnlineKeepOnlyActiveProfileChanged(ChangeEvent<bool> evt)
        {
            var s = AsrProjectSettings.instance;
            s.onlineData.keepOnlyActiveProfile = evt.newValue;
            s.SaveSettings();
        }

        private static void HandleOfflineBuildOnlyActiveProfileChanged(ChangeEvent<bool> evt)
        {
            var s = AsrProjectSettings.instance;
            s.offlineData.buildOnlyActiveProfile = evt.newValue;
            s.SaveSettings();
        }

        private static void HandleOnlineBuildOnlyActiveProfileChanged(ChangeEvent<bool> evt)
        {
            var s = AsrProjectSettings.instance;
            s.onlineData.buildOnlyActiveProfile = evt.newValue;
            s.SaveSettings();
        }

        private void BuildOfflinePresenters(AsrProjectSettings settings)
        {
            BuildOfflineAutoDeleteToggle(settings);

            var activeSection = _offlineContainer.Q<VisualElement>("activeProfileSection");
            _offlineActivePresenter = new ActiveProfilePresenter<AsrProfile>(settings.offlineData, settings, ModelPaths.GetAsrModelDir, ProfileFieldValidator.HasMissingFields);
            _offlineActivePresenter.Build(activeSection);

            _offlineImportSection = _offlineContainer.Q<VisualElement>("importSection");
            _offlineImportPresenter = new AsrImportPresenter(settings, HandleOfflineImportCompleted);
            _offlineImportPresenter.Build(_offlineImportSection);

            _offlineImportFromUrlButton = _offlineContainer.Q<Button>("importFromUrlButton");
            _offlineImportFromUrlButton.clicked += HandleOfflineImportFromUrlClicked;

            var listView = _offlineContainer.Q<ListView>("profilesListView");
            var addBtn = _offlineContainer.Q<Button>("addProfileButton");
            var removeBtn = _offlineContainer.Q<Button>("removeProfileButton");
            var detail = _offlineContainer.Q<VisualElement>("detailContent");

            _offlineListPresenter = new ProfileListPresenter<AsrProfile>(settings.offlineData, settings, ModelPaths.GetAsrModelDir, "model-list-item", ProfileFieldValidator.HasMissingFields);
            _offlineDetailPresenter = new AsrProfileDetailPresenter(detail, settings);
            _offlineDetailPresenter.SetListPresenter(_offlineListPresenter);

            _offlineListPresenter.SelectionChanged += HandleOfflineSelectionChanged;
            _offlineListPresenter.Build(listView, addBtn, removeBtn);
        }

        private void HandleOfflineImportFromUrlClicked() => _offlineImportSection?.ToggleInClassList("hidden");

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

        private void BuildOnlinePresenters(AsrProjectSettings settings)
        {
            BuildOnlineAutoDeleteToggle(settings);

            var activeSection = _onlineContainer.Q<VisualElement>("activeProfileSection");
            _onlineActivePresenter = new ActiveProfilePresenter<OnlineAsrProfile>(settings.onlineData, settings, ModelPaths.GetAsrModelDir, ProfileFieldValidator.HasMissingFields);
            _onlineActivePresenter.Build(activeSection);

            _onlineImportSection = _onlineContainer.Q<VisualElement>("importSection");
            _onlineImportPresenter = new OnlineAsrImportPresenter(settings, HandleOnlineImportCompleted);
            _onlineImportPresenter.Build(_onlineImportSection);

            _onlineImportFromUrlButton = _onlineContainer.Q<Button>("importFromUrlButton");
            _onlineImportFromUrlButton.clicked += HandleOnlineImportFromUrlClicked;

            var listView = _onlineContainer.Q<ListView>("profilesListView");
            var addBtn = _onlineContainer.Q<Button>("addProfileButton");
            var removeBtn = _onlineContainer.Q<Button>("removeProfileButton");
            var detail = _onlineContainer.Q<VisualElement>("detailContent");

            _onlineListPresenter = new ProfileListPresenter<OnlineAsrProfile>(settings.onlineData, settings, ModelPaths.GetAsrModelDir, "model-list-item", ProfileFieldValidator.HasMissingFields);
            _onlineDetailPresenter = new OnlineAsrProfileDetailPresenter(detail, settings);
            _onlineDetailPresenter.SetListPresenter(_onlineListPresenter);

            _onlineListPresenter.SelectionChanged += HandleOnlineSelectionChanged;
            _onlineListPresenter.Build(listView, addBtn, removeBtn);
        }

        private void HandleOnlineImportFromUrlClicked() => _onlineImportSection?.ToggleInClassList("hidden");

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
            _offlineKeepOnlyActiveProfileToggle?.UnregisterValueChangedCallback(
                HandleOfflineKeepOnlyActiveProfileChanged);
            _offlineKeepOnlyActiveProfileToggle = null;

            _offlineBuildOnlyActiveProfileToggle?.UnregisterValueChangedCallback(
                HandleOfflineBuildOnlyActiveProfileChanged);
            _offlineBuildOnlyActiveProfileToggle = null;

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
            _onlineKeepOnlyActiveProfileToggle?.UnregisterValueChangedCallback(
                HandleOnlineKeepOnlyActiveProfileChanged);
            _onlineKeepOnlyActiveProfileToggle = null;

            _onlineBuildOnlyActiveProfileToggle?.UnregisterValueChangedCallback(
                HandleOnlineBuildOnlyActiveProfileChanged);
            _onlineBuildOnlyActiveProfileToggle = null;

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