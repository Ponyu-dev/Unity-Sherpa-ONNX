using PonyuDev.SherpaOnnx.Editor.Common.UI;
using PonyuDev.SherpaOnnx.Editor.KwsInstall.Settings;
using PonyuDev.SherpaOnnx.Kws.Data;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.KwsInstall.Presenters
{
    /// <summary>
    /// Binds UI fields to <see cref="KwsProfile"/> properties via
    /// <see cref="KwsProfileField"/> enum — no lambdas required.
    /// </summary>
    internal sealed class KwsProfileFieldBinder
    {
        internal readonly KwsProfile Profile;
        private readonly KwsProjectSettings _settings;
        private readonly string _modelDir;

        internal KwsProfileFieldBinder(KwsProfile profile, KwsProjectSettings settings, string modelDir)
        {
            Profile = profile;
            _settings = settings;
            _modelDir = modelDir;
        }

        internal TextField BindText(string label, string value, KwsProfileField field)
        {
            var textField = new TextField(label) { value = value };
            var handler = new TextHandler(Profile, _settings, field);
            textField.RegisterValueChangedCallback(handler.Handle);
            return textField;
        }

        internal ModelObjectField BindFile(string label, string value, KwsProfileField field,
            string extension = "onnx", string keyword = "", bool isRequired = false)
        {
            var picker = new ModelObjectField(label, value, _modelDir, extension, keyword,
                isRequired: isRequired);
            var handler = new TextHandler(Profile, _settings, field);
            picker.RegisterFileChangedCallback(handler.SetValue);
            return picker;
        }

        internal FloatField BindFloat(string label, float value, KwsProfileField field)
        {
            var floatField = new FloatField(label) { value = value };
            var handler = new FloatHandler(Profile, _settings, field);
            floatField.RegisterValueChangedCallback(handler.Handle);
            return floatField;
        }

        internal TextField BindMultilineText(string label, string value, KwsProfileField field)
        {
            var textField = new TextField(label)
            {
                value = value,
                multiline = true
            };
            textField.style.minHeight = 60;
            textField.style.whiteSpace = WhiteSpace.Normal;
            var handler = new TextHandler(Profile, _settings, field);
            textField.RegisterValueChangedCallback(handler.Handle);
            return textField;
        }

        internal IntegerField BindInt(string label, int value, KwsProfileField field)
        {
            var intField = new IntegerField(label) { value = value };
            var handler = new IntHandler(Profile, _settings, field);
            intField.RegisterValueChangedCallback(handler.Handle);
            return intField;
        }

        // ── Handlers ──

        private sealed class TextHandler
        {
            private readonly KwsProfile _p;
            private readonly KwsProjectSettings _s;
            private readonly KwsProfileField _f;

            internal TextHandler(KwsProfile p, KwsProjectSettings s, KwsProfileField f)
            { _p = p; _s = s; _f = f; }

            internal void SetValue(string value)
            {
                KwsProfileFieldSetter.SetString(_p, _f, value);
                _s.SaveSettings();
            }

            internal void Handle(ChangeEvent<string> evt) => SetValue(evt.newValue);
        }

        private sealed class FloatHandler
        {
            private readonly KwsProfile _p;
            private readonly KwsProjectSettings _s;
            private readonly KwsProfileField _f;

            internal FloatHandler(KwsProfile p, KwsProjectSettings s, KwsProfileField f)
            { _p = p; _s = s; _f = f; }

            internal void Handle(ChangeEvent<float> evt)
            {
                KwsProfileFieldSetter.SetFloat(_p, _f, evt.newValue);
                _s.SaveSettings();
            }
        }

        private sealed class IntHandler
        {
            private readonly KwsProfile _p;
            private readonly KwsProjectSettings _s;
            private readonly KwsProfileField _f;

            internal IntHandler(KwsProfile p, KwsProjectSettings s, KwsProfileField f)
            { _p = p; _s = s; _f = f; }

            internal void Handle(ChangeEvent<int> evt)
            {
                KwsProfileFieldSetter.SetInt(_p, _f, evt.newValue);
                _s.SaveSettings();
            }
        }
    }
}
