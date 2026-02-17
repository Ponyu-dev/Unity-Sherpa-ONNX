using System;
using System.Collections.Generic;
using System.Linq;
using PonyuDev.SherpaOnnx.Common.Data;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.Common.Presenters
{
    /// <summary>
    /// Builds and manages the "Active profile" dropdown.
    /// Generic over any profile type implementing <see cref="IProfileData"/>.
    /// </summary>
    internal sealed class ActiveProfilePresenter<TProfile> : IDisposable
        where TProfile : IProfileData
    {
        private const string NoneLabel = "\u2014 None \u2014";

        private readonly ISettingsData<TProfile> _data;
        private readonly ISaveableSettings _settings;
        private PopupField<string> _dropdown;

        internal ActiveProfilePresenter(
            ISettingsData<TProfile> data, ISaveableSettings settings)
        {
            _data = data;
            _settings = settings;
        }

        internal void Build(VisualElement parent)
        {
            List<string> choices = BuildChoices();
            int savedIndex = _data.ActiveProfileIndex;
            string current = IndexToChoice(choices, savedIndex);

            _dropdown = new PopupField<string>(
                "Active profile", choices, current);
            _dropdown.RegisterValueChangedCallback(HandleChanged);
            parent.Add(_dropdown);
        }

        public void Dispose()
        {
            _dropdown?.UnregisterValueChangedCallback(HandleChanged);
            _dropdown = null;
        }

        internal void Refresh()
        {
            if (_dropdown == null) return;

            List<string> choices = BuildChoices();
            int savedIndex = _data.ActiveProfileIndex;
            string current = IndexToChoice(choices, savedIndex);

            _dropdown.choices = choices;
            _dropdown.SetValueWithoutNotify(current);
        }

        // ── Handlers ──

        private void HandleChanged(ChangeEvent<string> evt)
        {
            int index = ChoiceToIndex(evt.newValue);
            _data.ActiveProfileIndex = index;
            _settings.SaveSettings();
        }

        // ── Helpers ──

        private List<string> BuildChoices()
        {
            var list = new List<string> { NoneLabel };
            list.AddRange(_data.Profiles.Select(FormatName));
            return list;
        }

        private static string FormatName(TProfile profile)
        {
            return string.IsNullOrEmpty(profile.ProfileName)
                ? "(unnamed)" : profile.ProfileName;
        }

        private static string IndexToChoice(
            List<string> choices, int index)
        {
            int ci = index + 1;
            return ci >= 0 && ci < choices.Count
                ? choices[ci] : choices[0];
        }

        private int ChoiceToIndex(string choice)
        {
            if (choice == NoneLabel) return -1;

            List<TProfile> profiles = _data.Profiles;
            for (int i = 0; i < profiles.Count; i++)
            {
                if (FormatName(profiles[i]) == choice)
                    return i;
            }
            return -1;
        }
    }
}
