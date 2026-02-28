using System;
using System.Collections.Generic;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Editor.LibraryInstall.Helpers;
using UnityEditor;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.LibraryInstall
{
    internal sealed class SherpaOnnxSettingsView : IDisposable
    {
        private const string VersionFieldName = "versionField";
        private const string StrictToggleName = "strictValidationToggle";
        private const string MacToggleName = "macPostprocessToggle";
        private const string DebugEditorToggleName = "debugLogEditorToggle";
        private const string DebugRuntimeToggleName = "debugLogRuntimeToggle";

        private readonly string _mainUxmlPath;
        private readonly string _templateUxmlPath;

        private VisualElement _root;
        private TextField _versionField;
        private Toggle _strictToggle;
        private Toggle _macToggle;
        private Toggle _debugEditorToggle;
        private Toggle _debugRuntimeToggle;
        private Button _updateAllButton;

        private VisualTreeAsset _templateAsset;
        private readonly List<PlatformRowPresenter> _presenters = new(64);
        private readonly List<CacheSectionUi> _cacheSections = new(2);

        internal SherpaOnnxSettingsView(string mainUxmlPath, string templateUxmlPath)
        {
            _mainUxmlPath = mainUxmlPath;
            _templateUxmlPath = templateUxmlPath;
        }

        internal void Build(VisualElement hostRoot)
        {
            _root = hostRoot;

            VisualTreeAsset mainAsset =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(_mainUxmlPath);
            _templateAsset =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(_templateUxmlPath);

            if (mainAsset == null)
            {
                hostRoot.Add(new HelpBox(
                    "Main UXML not found: " + _mainUxmlPath, HelpBoxMessageType.Error));
                return;
            }

            if (_templateAsset == null)
            {
                hostRoot.Add(new HelpBox(
                    "Template UXML not found: " + _templateUxmlPath, HelpBoxMessageType.Error));
                return;
            }

            hostRoot.Clear();
            hostRoot.Add(mainAsset.CloneTree());

            _versionField = hostRoot.Q<TextField>(VersionFieldName);
            _strictToggle = hostRoot.Q<Toggle>(StrictToggleName);
            _macToggle = hostRoot.Q<Toggle>(MacToggleName);
            _debugEditorToggle = hostRoot.Q<Toggle>(DebugEditorToggleName);
            _debugRuntimeToggle = hostRoot.Q<Toggle>(DebugRuntimeToggleName);

            BindSettingsToUi();
            SubscribeUi();

            AddUpdateAllButton();
            AddManagedDll();
            BuildPlatformRows();

            RefreshUpdateAllButton();
        }

        public void Dispose()
        {
            UnsubscribeUi();

            for (int i = 0; i < _presenters.Count; i++)
                _presenters[i].Dispose();
            _presenters.Clear();

            for (int i = 0; i < _cacheSections.Count; i++)
                _cacheSections[i].Dispose();
            _cacheSections.Clear();

            _templateAsset = null;
            _versionField = null;
            _strictToggle = null;
            _macToggle = null;
            _debugEditorToggle = null;
            _debugRuntimeToggle = null;
            _updateAllButton = null;

            _root = null;
        }

        private void AddUpdateAllButton()
        {
            _updateAllButton = new Button(HandleUpdateAllClicked) { text = "Update All" };
            _updateAllButton.AddToClassList("btn");
            _updateAllButton.AddToClassList("btn-warning");

            VisualElement parent = _versionField?.parent;
            if (parent != null)
            {
                int idx = parent.IndexOf(_versionField) + 1;
                parent.Insert(idx, _updateAllButton);
            }
            else
            {
                _root?.Add(_updateAllButton);
            }
        }

        private void AddManagedDll()
        {
            VisualElement rowRoot = _templateAsset.CloneTree();
            var presenter = new PlatformRowPresenter(
                LibraryPlatforms.ManagedLibrary, GetVersion);
            presenter.Build(rowRoot);
            _presenters.Add(presenter);
            _root.Add(rowRoot);
        }

        private void BuildPlatformRows()
        {
            foreach (LibraryPlatform platform in LibraryPlatforms.Platforms)
            {
                var foldout = new Foldout { text = platform.PlatformName, value = true };

                CacheSectionUi cacheSection = CacheSectionUi.CreateForPlatform(platform.PlatformName);
                if (cacheSection != null)
                {
                    cacheSection.Build(foldout);
                    _cacheSections.Add(cacheSection);
                }

                foreach (LibraryArch arch in platform.Arches)
                {
                    VisualElement rowRoot = _templateAsset.CloneTree();
                    var presenter = new PlatformRowPresenter(arch, GetVersion);
                    presenter.Build(rowRoot);

                    _presenters.Add(presenter);
                    foldout.Add(rowRoot);
                }

                _root.Add(foldout);
            }
        }

        private void BindSettingsToUi()
        {
            var s = SherpaOnnxProjectSettings.instance;

            if (_versionField != null)
                _versionField.value = s.version;
            if (_strictToggle != null)
                _strictToggle.value = s.strictValidation;
            if (_macToggle != null)
                _macToggle.value = s.macPostprocess;
            if (_debugEditorToggle != null)
                _debugEditorToggle.value = s.debugLogEditor;
            if (_debugRuntimeToggle != null)
                _debugRuntimeToggle.value = s.debugLogRuntime;
        }

        private void SubscribeUi()
        {
            _root?.RegisterCallback<DetachFromPanelEvent>(HandleDetachFromPanel);
            _versionField?.RegisterValueChangedCallback(HandleVersionChanged);
            _strictToggle?.RegisterValueChangedCallback(HandleStrictChanged);
            _macToggle?.RegisterValueChangedCallback(HandleMacChanged);
            _debugEditorToggle?.RegisterValueChangedCallback(HandleDebugEditorChanged);
            _debugRuntimeToggle?.RegisterValueChangedCallback(HandleDebugRuntimeChanged);
        }

        private void UnsubscribeUi()
        {
            _root?.UnregisterCallback<DetachFromPanelEvent>(HandleDetachFromPanel);
            _versionField?.UnregisterValueChangedCallback(HandleVersionChanged);
            _strictToggle?.UnregisterValueChangedCallback(HandleStrictChanged);
            _macToggle?.UnregisterValueChangedCallback(HandleMacChanged);
            _debugEditorToggle?.UnregisterValueChangedCallback(HandleDebugEditorChanged);
            _debugRuntimeToggle?.UnregisterValueChangedCallback(HandleDebugRuntimeChanged);
        }

        private void HandleDetachFromPanel(DetachFromPanelEvent evt) => Dispose();

        private void HandleVersionChanged(ChangeEvent<string> evt)
        {
            var s = SherpaOnnxProjectSettings.instance;
            s.version = evt.newValue;
            s.SaveSettings();

            for (int i = 0; i < _presenters.Count; i++)
                _presenters[i].RefreshStatus();

            RefreshUpdateAllButton();
        }

        private void HandleUpdateAllClicked()
        {
            for (int i = 0; i < _presenters.Count; i++)
            {
                if (_presenters[i].NeedsUpdate())
                    _presenters[i].TriggerInstall();
            }
        }

        private void RefreshUpdateAllButton()
        {
            if (_updateAllButton == null)
                return;

            bool anyNeedsUpdate = false;
            for (int i = 0; i < _presenters.Count; i++)
            {
                if (_presenters[i].NeedsUpdate())
                {
                    anyNeedsUpdate = true;
                    break;
                }
            }

            _updateAllButton.SetEnabled(anyNeedsUpdate);
        }

        private void HandleStrictChanged(ChangeEvent<bool> evt)
        {
            var s = SherpaOnnxProjectSettings.instance;
            s.strictValidation = evt.newValue;
            s.SaveSettings();
        }

        private void HandleMacChanged(ChangeEvent<bool> evt)
        {
            var s = SherpaOnnxProjectSettings.instance;
            s.macPostprocess = evt.newValue;
            s.SaveSettings();
        }

        private void HandleDebugEditorChanged(ChangeEvent<bool> evt)
        {
            var s = SherpaOnnxProjectSettings.instance;
            s.debugLogEditor = evt.newValue;
            s.SaveSettings();
            SherpaOnnxLog.EditorEnabled = evt.newValue;
        }

        private void HandleDebugRuntimeChanged(ChangeEvent<bool> evt)
        {
            var s = SherpaOnnxProjectSettings.instance;
            s.debugLogRuntime = evt.newValue;
            s.SaveSettings();
            SherpaOnnxLog.RuntimeEnabled = evt.newValue;
        }

        private static string GetVersion() =>
            SherpaOnnxProjectSettings.instance.version;
    }
}