using System;
using System.Collections.Generic;
using PonyuDev.SherpaOnnx.Editor.Common;
using PonyuDev.SherpaOnnx.Editor.TtsInstall.Import;
using PonyuDev.SherpaOnnx.Editor.TtsInstall.Settings;
using PonyuDev.SherpaOnnx.Tts.Data;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.TtsInstall.Presenters
{
    /// <summary>
    /// Manages the profile ListView and Add/Remove buttons.
    /// </summary>
    internal sealed class TtsProfileListPresenter : IDisposable
    {
        private readonly TtsProjectSettings _settings;
        private ListView _listView;
        private Button _addButton;
        private Button _removeButton;

        internal event Action<int> SelectionChanged;

        internal TtsProfileListPresenter(TtsProjectSettings settings)
        {
            _settings = settings;
        }

        internal void Build(ListView listView, Button addButton, Button removeButton)
        {
            _listView = listView;
            _addButton = addButton;
            _removeButton = removeButton;

            _listView.makeItem = MakeItem;
            _listView.bindItem = BindItem;
            _listView.selectionChanged += HandleSelectionChanged;
            _addButton.clicked += HandleAdd;
            _removeButton.clicked += HandleRemove;

            RefreshList();
        }

        public void Dispose()
        {
            if (_listView != null)
                _listView.selectionChanged -= HandleSelectionChanged;
            if (_addButton != null)
                _addButton.clicked -= HandleAdd;
            if (_removeButton != null)
                _removeButton.clicked -= HandleRemove;

            _listView = null;
            _addButton = null;
            _removeButton = null;
        }

        internal void RefreshList()
        {
            List<TtsProfile> profiles = _settings.data.profiles;
            _listView.itemsSource = profiles;
            _listView.Rebuild();
            _removeButton?.SetEnabled(_listView.selectedIndex >= 0);
        }

        private static VisualElement MakeItem()
        {
            var label = new Label();
            label.AddToClassList("tts-list-item");
            return label;
        }

        private void BindItem(VisualElement element, int index)
        {
            var label = (Label)element;
            List<TtsProfile> profiles = _settings.data.profiles;

            label.text = index < profiles.Count
                ? profiles[index].profileName
                : "—";
        }

        private void HandleSelectionChanged(IEnumerable<object> selection)
        {
            int index = _listView.selectedIndex;
            _removeButton?.SetEnabled(index >= 0);
            SelectionChanged?.Invoke(index);
            PingSelectedProfile(index);
        }

        private void HandleAdd()
        {
            _settings.data.profiles.Add(new TtsProfile());
            _settings.SaveSettings();
            RefreshList();

            int last = _settings.data.profiles.Count - 1;
            _listView.selectedIndex = last;
        }

        private void HandleRemove()
        {
            int index = _listView.selectedIndex;
            if (index < 0 || index >= _settings.data.profiles.Count)
                return;

            TtsProfile profile = _settings.data.profiles[index];
            string modelDir = TtsModelPaths.GetModelDir(profile.profileName);
            ModelFileService.DeleteModelDirectory(modelDir);

            _settings.data.profiles.RemoveAt(index);
            AdjustActiveIndexAfterRemove(index);
            _settings.SaveSettings();

            _listView.selectedIndex = -1;
            SelectionChanged?.Invoke(-1);
            RefreshList();
        }

        private void PingSelectedProfile(int index)
        {
            if (index < 0 || index >= _settings.data.profiles.Count)
                return;

            string profileName = _settings.data.profiles[index].profileName;
            if (string.IsNullOrEmpty(profileName))
                return;

            string modelDir = TtsModelPaths.GetModelDir(profileName);
            ModelFileService.PingFirstAsset(modelDir);
        }

        private void AdjustActiveIndexAfterRemove(int removedIndex)
        {
            int active = _settings.data.activeProfileIndex;

            if (active == removedIndex)
                _settings.data.activeProfileIndex = -1;
            else if (active > removedIndex)
                _settings.data.activeProfileIndex = active - 1;
        }
    }
}