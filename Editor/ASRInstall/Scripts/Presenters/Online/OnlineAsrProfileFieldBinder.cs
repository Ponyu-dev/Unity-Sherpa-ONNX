using PonyuDev.SherpaOnnx.Asr.Online.Data;
using PonyuDev.SherpaOnnx.Editor.AsrInstall.Settings;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.AsrInstall.Presenters.Online
{
    /// <summary>
    /// Binds UI fields to <see cref="OnlineAsrProfile"/> properties —
    /// no lambdas.
    /// </summary>
    internal sealed class OnlineAsrProfileFieldBinder
    {
        internal readonly OnlineAsrProfile Profile;
        private readonly AsrProjectSettings _settings;

        internal OnlineAsrProfileFieldBinder(OnlineAsrProfile profile, AsrProjectSettings settings)
        {
            Profile = profile;
            _settings = settings;
        }

        internal TextField BindText(string label, string value, OnlineAsrProfileField field)
        {
            var textField = new TextField(label) { value = value };
            var handler = new TextHandler(Profile, _settings, field);
            textField.RegisterValueChangedCallback(handler.Handle);
            return textField;
        }

        internal FloatField BindFloat(string label, float value, OnlineAsrProfileField field)
        {
            var floatField = new FloatField(label) { value = value };
            var handler = new FloatHandler(Profile, _settings, field);
            floatField.RegisterValueChangedCallback(handler.Handle);
            return floatField;
        }

        internal IntegerField BindInt(string label, int value, OnlineAsrProfileField field)
        {
            var intField = new IntegerField(label) { value = value };
            var handler = new IntHandler(Profile, _settings, field);
            intField.RegisterValueChangedCallback(handler.Handle);
            return intField;
        }

        // ── Handlers ──

        private sealed class TextHandler
        {
            private readonly OnlineAsrProfile _p;
            private readonly AsrProjectSettings _s;
            private readonly OnlineAsrProfileField _f;

            internal TextHandler(OnlineAsrProfile p, AsrProjectSettings s, OnlineAsrProfileField f)
            { _p = p; _s = s; _f = f; }

            internal void Handle(ChangeEvent<string> evt)
            {
                OnlineAsrProfileFieldSetter.SetString(_p, _f, evt.newValue);
                _s.SaveSettings();
            }
        }

        private sealed class FloatHandler
        {
            private readonly OnlineAsrProfile _p;
            private readonly AsrProjectSettings _s;
            private readonly OnlineAsrProfileField _f;

            internal FloatHandler(OnlineAsrProfile p, AsrProjectSettings s, OnlineAsrProfileField f)
            { _p = p; _s = s; _f = f; }

            internal void Handle(ChangeEvent<float> evt)
            {
                OnlineAsrProfileFieldSetter.SetFloat(_p, _f, evt.newValue);
                _s.SaveSettings();
            }
        }

        private sealed class IntHandler
        {
            private readonly OnlineAsrProfile _p;
            private readonly AsrProjectSettings _s;
            private readonly OnlineAsrProfileField _f;

            internal IntHandler(OnlineAsrProfile p, AsrProjectSettings s, OnlineAsrProfileField f)
            { _p = p; _s = s; _f = f; }

            internal void Handle(ChangeEvent<int> evt)
            {
                OnlineAsrProfileFieldSetter.SetInt(_p, _f, evt.newValue);
                _s.SaveSettings();
            }
        }
    }
}
