using PonyuDev.SherpaOnnx.Editor.VadInstall.Settings;
using PonyuDev.SherpaOnnx.Vad.Data;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.VadInstall.Presenters
{
    /// <summary>
    /// Binds UI fields to <see cref="VadProfile"/> properties via
    /// <see cref="VadProfileField"/> enum — no lambdas required.
    /// </summary>
    internal sealed class VadProfileFieldBinder
    {
        internal readonly VadProfile Profile;
        private readonly VadProjectSettings _settings;

        internal VadProfileFieldBinder(VadProfile profile, VadProjectSettings settings)
        {
            Profile = profile;
            _settings = settings;
        }

        internal TextField BindText(string label, string value, VadProfileField field)
        {
            var textField = new TextField(label) { value = value };
            var handler = new TextHandler(Profile, _settings, field);
            textField.RegisterValueChangedCallback(handler.Handle);
            return textField;
        }

        internal FloatField BindFloat(string label, float value, VadProfileField field)
        {
            var floatField = new FloatField(label) { value = value };
            var handler = new FloatHandler(Profile, _settings, field);
            floatField.RegisterValueChangedCallback(handler.Handle);
            return floatField;
        }

        internal IntegerField BindInt(string label, int value, VadProfileField field)
        {
            var intField = new IntegerField(label) { value = value };
            var handler = new IntHandler(Profile, _settings, field);
            intField.RegisterValueChangedCallback(handler.Handle);
            return intField;
        }

        // ── Handlers ──

        private sealed class TextHandler
        {
            private readonly VadProfile _p;
            private readonly VadProjectSettings _s;
            private readonly VadProfileField _f;

            internal TextHandler(VadProfile p, VadProjectSettings s, VadProfileField f)
            { _p = p; _s = s; _f = f; }

            internal void Handle(ChangeEvent<string> evt)
            {
                VadProfileFieldSetter.SetString(_p, _f, evt.newValue);
                _s.SaveSettings();
            }
        }

        private sealed class FloatHandler
        {
            private readonly VadProfile _p;
            private readonly VadProjectSettings _s;
            private readonly VadProfileField _f;

            internal FloatHandler(VadProfile p, VadProjectSettings s, VadProfileField f)
            { _p = p; _s = s; _f = f; }

            internal void Handle(ChangeEvent<float> evt)
            {
                VadProfileFieldSetter.SetFloat(_p, _f, evt.newValue);
                _s.SaveSettings();
            }
        }

        private sealed class IntHandler
        {
            private readonly VadProfile _p;
            private readonly VadProjectSettings _s;
            private readonly VadProfileField _f;

            internal IntHandler(VadProfile p, VadProjectSettings s, VadProfileField f)
            { _p = p; _s = s; _f = f; }

            internal void Handle(ChangeEvent<int> evt)
            {
                VadProfileFieldSetter.SetInt(_p, _f, evt.newValue);
                _s.SaveSettings();
            }
        }
    }
}
