using System;
using System.Collections.Generic;
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

        private readonly string _mainUxmlPath;
        private readonly string _templateUxmlPath;

        private VisualElement _root;
        private TextField _versionField;
        private Toggle _strictToggle;
        private Toggle _macToggle;

        private VisualTreeAsset _templateAsset;

        private PlatformRowPresenter _managedDllPresenter;
        private readonly List<PlatformRowPresenter> _presenters = new(64);

        private Button _cleanCacheButton;
        private Button _openCacheButton;

        internal SherpaOnnxSettingsView(string mainUxmlPath, string templateUxmlPath)
        {
            _mainUxmlPath = mainUxmlPath;
            _templateUxmlPath = templateUxmlPath;
        }

        internal void Build(VisualElement hostRoot)
        {
            _root = hostRoot;

            VisualTreeAsset mainAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(_mainUxmlPath);
            _templateAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(_templateUxmlPath);

            if (mainAsset == null)
            {
                hostRoot.Add(new HelpBox("Main UXML not found: " + _mainUxmlPath, HelpBoxMessageType.Error));
                return;
            }

            if (_templateAsset == null)
            {
                hostRoot.Add(new HelpBox("Template UXML not found: " + _templateUxmlPath, HelpBoxMessageType.Error));
                return;
            }

            hostRoot.Clear();
            hostRoot.Add(mainAsset.CloneTree());

            _versionField = hostRoot.Q<TextField>(VersionFieldName);
            _strictToggle = hostRoot.Q<Toggle>(StrictToggleName);
            _macToggle = hostRoot.Q<Toggle>(MacToggleName);

            BindSettingsToUi();
            SubscribeUi();

            AddManagedDll();
            BuildPlatformRows();
        }

        private void AddManagedDll()
        {
            VisualElement rowRoot = _templateAsset.CloneTree();
            _managedDllPresenter = new PlatformRowPresenter(LibraryPlatforms.ManagedLibrary, GetVersion);
            _managedDllPresenter.Build(rowRoot);
            _presenters.Add(_managedDllPresenter);
            _root.Add(rowRoot);
        }

        private void BuildPlatformRows()
        {
            foreach (LibraryPlatform platform in LibraryPlatforms.Platforms)
            {
                var foldout = new Foldout
                {
                    text = platform.PlatformName,
                    value = true
                };

                if (platform.PlatformName == "Android")
                {
                    var helpBox = new HelpBox(
                        "Android libraries are downloaded as a single archive containing all architectures. "
                        + "The extracted archive is cached so each architecture can be installed without re-downloading. "
                        + "Use 'Clean cache' to remove the cached archive and free disk space.",
                        HelpBoxMessageType.Info);
                    foldout.Add(helpBox);

                    _cleanCacheButton = new Button(HandleCleanAndroidCache)
                    {
                        text = "Clean cache"
                    };
                    _cleanCacheButton.SetEnabled(AndroidArchiveCache.IsReady);
                    foldout.Add(_cleanCacheButton);

                    _openCacheButton = new Button(HandleOpenAndroidCache)
                    {
                        text = "Open cache"
                    };
                    _openCacheButton.SetEnabled(AndroidArchiveCache.IsReady);
                    foldout.Add(_openCacheButton);
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

        private void HandleCleanAndroidCache()
        {
            AndroidArchiveCache.Clean();
        }

        private void HandleCacheChanged()
        {
            bool ready = AndroidArchiveCache.IsReady;
            _cleanCacheButton?.SetEnabled(ready);
            _openCacheButton?.SetEnabled(ready);
        }

        private static void HandleOpenAndroidCache()
        {
            EditorUtility.RevealInFinder(AndroidArchiveCache.CachePath);
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
        }

        private void SubscribeUi()
        {
            _root?.RegisterCallback<DetachFromPanelEvent>(HandleDetachFromPanel);
            _versionField?.RegisterValueChangedCallback(HandleVersionChanged);
            _strictToggle?.RegisterValueChangedCallback(HandleStrictChanged);
            _macToggle?.RegisterValueChangedCallback(HandleMacChanged);
            AndroidArchiveCache.OnCacheChanged += HandleCacheChanged;
        }

        private void UnsubscribeUi()
        {
            _root?.UnregisterCallback<DetachFromPanelEvent>(HandleDetachFromPanel);
            _versionField?.UnregisterValueChangedCallback(HandleVersionChanged);
            _strictToggle?.UnregisterValueChangedCallback(HandleStrictChanged);
            _macToggle?.UnregisterValueChangedCallback(HandleMacChanged);
            AndroidArchiveCache.OnCacheChanged -= HandleCacheChanged;
        }

        private void HandleDetachFromPanel(DetachFromPanelEvent evt)
        {
            Dispose();
        }

        private void HandleVersionChanged(ChangeEvent<string> evt)
        {
            var s = SherpaOnnxProjectSettings.instance;
            s.version = evt.newValue;
            s.SaveSettings();
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

        private static string GetVersion()
        {
            return SherpaOnnxProjectSettings.instance.version;
        }

        public void Dispose()
        {
            UnsubscribeUi();

            for (int i = 0; i < _presenters.Count; i++)
                _presenters[i].Dispose();
            _presenters.Clear();

            _managedDllPresenter = null;
            _templateAsset = null;

            _versionField = null;
            _strictToggle = null;
            _macToggle = null;
            _cleanCacheButton = null;
            _openCacheButton = null;
            _root = null;
        }
    }
}