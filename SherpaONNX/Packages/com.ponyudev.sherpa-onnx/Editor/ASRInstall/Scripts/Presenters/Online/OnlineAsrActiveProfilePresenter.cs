using System;
using System.Collections.Generic;
using System.Linq;
using PonyuDev.SherpaOnnx.Asr.Online.Data;
using PonyuDev.SherpaOnnx.Editor.AsrInstall.Settings;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.AsrInstall.Presenters.Online
{
    /// <summary>
    /// Builds and manages the "Active profile" dropdown for online ASR.
    /// </summary>
    internal sealed class OnlineAsrActiveProfilePresenter : IDisposable
    {
        private const string NoneLabel = "— None —";

        private readonly AsrProjectSettings _settings;
        private PopupField<string> _dropdown;

        internal OnlineAsrActiveProfilePresenter(
            AsrProjectSettings settings)
        {
            _settings = settings;
        }

        internal void Build(VisualElement parent)
        {
            List<string> choices = BuildChoices();
            int savedIndex = _settings.onlineData.activeProfileIndex;
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
            int savedIndex = _settings.onlineData.activeProfileIndex;
            string current = IndexToChoice(choices, savedIndex);

            _dropdown.choices = choices;
            _dropdown.SetValueWithoutNotify(current);
        }

        // ── Handlers ──

        private void HandleChanged(ChangeEvent<string> evt)
        {
            int index = ChoiceToIndex(evt.newValue);
            _settings.onlineData.activeProfileIndex = index;
            _settings.SaveSettings();
        }

        // ── Helpers ──

        private List<string> BuildChoices()
        {
            var list = new List<string> { NoneLabel };
            list.AddRange(
                _settings.onlineData.profiles.Select(FormatName));
            return list;
        }

        private static string FormatName(OnlineAsrProfile profile)
        {
            return string.IsNullOrEmpty(profile.profileName)
                ? "(unnamed)" : profile.profileName;
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

            List<OnlineAsrProfile> profiles =
                _settings.onlineData.profiles;
            for (int i = 0; i < profiles.Count; i++)
            {
                if (FormatName(profiles[i]) == choice)
                    return i;
            }
            return -1;
        }
    }
}
