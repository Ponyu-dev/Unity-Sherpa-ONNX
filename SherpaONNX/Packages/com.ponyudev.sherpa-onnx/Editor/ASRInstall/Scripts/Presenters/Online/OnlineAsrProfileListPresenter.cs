using System;
using System.Collections.Generic;
using PonyuDev.SherpaOnnx.Asr.Online.Data;
using PonyuDev.SherpaOnnx.Editor.AsrInstall.Import;
using PonyuDev.SherpaOnnx.Editor.AsrInstall.Settings;
using PonyuDev.SherpaOnnx.Editor.Common;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.AsrInstall.Presenters.Online
{
    /// <summary>
    /// Manages the online ASR profile ListView and Add/Remove buttons.
    /// </summary>
    internal sealed class OnlineAsrProfileListPresenter : IDisposable
    {
        private readonly AsrProjectSettings _settings;
        private ListView _listView;
        private Button _addButton;
        private Button _removeButton;

        internal event Action<int> SelectionChanged;

        internal OnlineAsrProfileListPresenter(
            AsrProjectSettings settings)
        {
            _settings = settings;
        }

        internal void Build(
            ListView listView, Button addButton, Button removeButton)
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
            List<OnlineAsrProfile> profiles =
                _settings.onlineData.profiles;
            _listView.itemsSource = profiles;
            _listView.Rebuild();
            _removeButton?.SetEnabled(_listView.selectedIndex >= 0);
        }

        private static VisualElement MakeItem()
        {
            var label = new Label();
            label.AddToClassList("asr-list-item");
            return label;
        }

        private void BindItem(VisualElement element, int index)
        {
            var label = (Label)element;
            List<OnlineAsrProfile> profiles =
                _settings.onlineData.profiles;

            label.text = index < profiles.Count
                ? profiles[index].profileName : "—";
        }

        private void HandleSelectionChanged(
            IEnumerable<object> selection)
        {
            int index = _listView.selectedIndex;
            _removeButton?.SetEnabled(index >= 0);
            SelectionChanged?.Invoke(index);
            PingSelectedProfile(index);
        }

        private void HandleAdd()
        {
            _settings.onlineData.profiles.Add(new OnlineAsrProfile());
            _settings.SaveSettings();
            RefreshList();

            int last = _settings.onlineData.profiles.Count - 1;
            _listView.selectedIndex = last;
        }

        private void HandleRemove()
        {
            int index = _listView.selectedIndex;
            if (index < 0
                || index >= _settings.onlineData.profiles.Count)
                return;

            OnlineAsrProfile profile =
                _settings.onlineData.profiles[index];
            string modelDir = AsrModelPaths.GetModelDir(
                profile.profileName);
            ModelFileService.DeleteModelDirectory(modelDir);

            _settings.onlineData.profiles.RemoveAt(index);
            AdjustActiveIndexAfterRemove(index);
            _settings.SaveSettings();

            _listView.selectedIndex = -1;
            SelectionChanged?.Invoke(-1);
            RefreshList();
        }

        private void PingSelectedProfile(int index)
        {
            if (index < 0
                || index >= _settings.onlineData.profiles.Count)
                return;

            string profileName =
                _settings.onlineData.profiles[index].profileName;
            if (string.IsNullOrEmpty(profileName)) return;

            string modelDir = AsrModelPaths.GetModelDir(profileName);
            ModelFileService.PingFirstAsset(modelDir);
        }

        private void AdjustActiveIndexAfterRemove(int removedIndex)
        {
            int active = _settings.onlineData.activeProfileIndex;

            if (active == removedIndex)
                _settings.onlineData.activeProfileIndex = -1;
            else if (active > removedIndex)
                _settings.onlineData.activeProfileIndex = active - 1;
        }
    }
}
