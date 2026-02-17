using System;
using System.Collections.Generic;
using PonyuDev.SherpaOnnx.Common.Data;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.Common.Presenters
{
    /// <summary>
    /// Manages a profile ListView with Add/Remove buttons.
    /// Generic over any profile type implementing <see cref="IProfileData"/>.
    /// </summary>
    internal sealed class ProfileListPresenter<TProfile> : IDisposable
        where TProfile : class, IProfileData, new()
    {
        private readonly ISettingsData<TProfile> _data;
        private readonly ISaveableSettings _settings;
        private readonly Func<string, string> _getModelDir;
        private readonly string _itemCssClass;

        private ListView _listView;
        private Button _addButton;
        private Button _removeButton;

        internal event Action<int> SelectionChanged;

        internal ProfileListPresenter(
            ISettingsData<TProfile> data,
            ISaveableSettings settings,
            Func<string, string> getModelDir,
            string itemCssClass)
        {
            _data = data;
            _settings = settings;
            _getModelDir = getModelDir;
            _itemCssClass = itemCssClass;
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
            List<TProfile> profiles = _data.Profiles;
            _listView.itemsSource = profiles;
            _listView.Rebuild();
            _removeButton?.SetEnabled(_listView.selectedIndex >= 0);
        }

        // ── Handlers ──

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
            _data.Profiles.Add(new TProfile());
            _settings.SaveSettings();
            RefreshList();

            int last = _data.Profiles.Count - 1;
            _listView.selectedIndex = last;
        }

        private void HandleRemove()
        {
            int index = _listView.selectedIndex;
            if (index < 0 || index >= _data.Profiles.Count)
                return;

            TProfile profile = _data.Profiles[index];
            string modelDir = _getModelDir(profile.ProfileName);
            ModelFileService.DeleteModelDirectory(modelDir);

            _data.Profiles.RemoveAt(index);
            AdjustActiveIndexAfterRemove(index);
            _settings.SaveSettings();

            _listView.selectedIndex = -1;
            SelectionChanged?.Invoke(-1);
            RefreshList();
        }

        // ── Helpers ──

        private VisualElement MakeItem()
        {
            var label = new Label();
            label.AddToClassList(_itemCssClass);
            return label;
        }

        private void BindItem(VisualElement element, int index)
        {
            var label = (Label)element;
            List<TProfile> profiles = _data.Profiles;

            label.text = index < profiles.Count
                ? profiles[index].ProfileName : "\u2014";
        }

        private void PingSelectedProfile(int index)
        {
            if (index < 0 || index >= _data.Profiles.Count)
                return;

            string profileName = _data.Profiles[index].ProfileName;
            if (string.IsNullOrEmpty(profileName))
                return;

            string modelDir = _getModelDir(profileName);
            ModelFileService.PingFirstAsset(modelDir);
        }

        private void AdjustActiveIndexAfterRemove(int removedIndex)
        {
            int active = _data.ActiveProfileIndex;

            if (active == removedIndex)
                _data.ActiveProfileIndex = -1;
            else if (active > removedIndex)
                _data.ActiveProfileIndex = active - 1;
        }
    }
}
