using PonyuDev.SherpaOnnx.Asr.Offline.Data;
using PonyuDev.SherpaOnnx.Editor.AsrInstall.Settings;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.AsrInstall.Presenters.Offline
{
    /// <summary>
    /// Binds UI fields to <see cref="AsrProfile"/> properties via
    /// <see cref="AsrProfileField"/> enum — no lambdas.
    /// </summary>
    internal sealed class AsrProfileFieldBinder
    {
        internal readonly AsrProfile Profile;
        private readonly AsrProjectSettings _settings;

        internal AsrProfileFieldBinder(AsrProfile profile, AsrProjectSettings settings)
        {
            Profile = profile;
            _settings = settings;
        }

        internal TextField BindText(string label, string value, AsrProfileField field)
        {
            var textField = new TextField(label) { value = value };
            var handler = new TextHandler(Profile, _settings, field);
            textField.RegisterValueChangedCallback(handler.Handle);
            return textField;
        }

        internal FloatField BindFloat(string label, float value, AsrProfileField field)
        {
            var floatField = new FloatField(label) { value = value };
            var handler = new FloatHandler(Profile, _settings, field);
            floatField.RegisterValueChangedCallback(handler.Handle);
            return floatField;
        }

        internal IntegerField BindInt(string label, int value, AsrProfileField field)
        {
            var intField = new IntegerField(label) { value = value };
            var handler = new IntHandler(Profile, _settings, field);
            intField.RegisterValueChangedCallback(handler.Handle);
            return intField;
        }

        // ── Handlers ──

        private sealed class TextHandler
        {
            private readonly AsrProfile _p;
            private readonly AsrProjectSettings _s;
            private readonly AsrProfileField _f;

            internal TextHandler(AsrProfile p, AsrProjectSettings s, AsrProfileField f)
            { _p = p; _s = s; _f = f; }

            internal void Handle(ChangeEvent<string> evt)
            {
                AsrProfileFieldSetter.SetString(_p, _f, evt.newValue);
                _s.SaveSettings();
            }
        }

        private sealed class FloatHandler
        {
            private readonly AsrProfile _p;
            private readonly AsrProjectSettings _s;
            private readonly AsrProfileField _f;

            internal FloatHandler(AsrProfile p, AsrProjectSettings s, AsrProfileField f)
            { _p = p; _s = s; _f = f; }

            internal void Handle(ChangeEvent<float> evt)
            {
                AsrProfileFieldSetter.SetFloat(_p, _f, evt.newValue);
                _s.SaveSettings();
            }
        }

        private sealed class IntHandler
        {
            private readonly AsrProfile _p;
            private readonly AsrProjectSettings _s;
            private readonly AsrProfileField _f;

            internal IntHandler(AsrProfile p, AsrProjectSettings s, AsrProfileField f)
            { _p = p; _s = s; _f = f; }

            internal void Handle(ChangeEvent<int> evt)
            {
                AsrProfileFieldSetter.SetInt(_p, _f, evt.newValue);
                _s.SaveSettings();
            }
        }
    }
}
