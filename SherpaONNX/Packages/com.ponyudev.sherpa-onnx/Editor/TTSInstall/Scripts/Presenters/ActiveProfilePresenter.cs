using System;
using System.Collections.Generic;
using System.Linq;
using PonyuDev.SherpaOnnx.Editor.TtsInstall.Settings;
using PonyuDev.SherpaOnnx.Tts.Data;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.TtsInstall.Presenters
{
    /// <summary>
    /// Builds and manages the "Active profile" dropdown above the toolbar.
    /// </summary>
    internal sealed class ActiveProfilePresenter : IDisposable
    {
        private const string NoneLabel = "— None —";

        private readonly TtsProjectSettings _settings;
        private PopupField<string> _dropdown;

        internal ActiveProfilePresenter(TtsProjectSettings settings)
        {
            _settings = settings;
        }

        internal void Build(VisualElement parent)
        {
            List<string> choices = BuildChoices();
            int savedIndex = _settings.data.activeProfileIndex;
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
            int savedIndex = _settings.data.activeProfileIndex;
            string current = IndexToChoice(choices, savedIndex);

            _dropdown.choices = choices;
            _dropdown.SetValueWithoutNotify(current);
        }

        // ── Handlers ──

        private void HandleChanged(ChangeEvent<string> evt)
        {
            string selected = evt.newValue;
            int index = ChoiceToIndex(selected);

            _settings.data.activeProfileIndex = index;
            _settings.SaveSettings();
        }

        // ── Helpers ──

        private List<string> BuildChoices()
        {
            var list = new List<string> { NoneLabel };
            list.AddRange(_settings.data.profiles.Select(FormatProfileName));
            return list;
        }

        private static string FormatProfileName(TtsProfile profile)
        {
            return string.IsNullOrEmpty(profile.profileName)
                ? "(unnamed)"
                : profile.profileName;
        }

        private static string IndexToChoice(List<string> choices, int index)
        {
            int choiceIndex = index + 1;

            return choiceIndex >= 0 && choiceIndex < choices.Count
                ? choices[choiceIndex]
                : choices[0];
        }

        private int ChoiceToIndex(string choice)
        {
            if (choice == NoneLabel) return -1;

            List<TtsProfile> profiles = _settings.data.profiles;

            for (int i = 0; i < profiles.Count; i++)
            {
                if (FormatProfileName(profiles[i]) == choice)
                    return i;
            }

            return -1;
        }
    }
}
