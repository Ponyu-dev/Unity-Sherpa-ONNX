using PonyuDev.SherpaOnnx.Asr.Offline.Data;
using PonyuDev.SherpaOnnx.Editor.AsrInstall.Settings;
using PonyuDev.SherpaOnnx.Editor.Common.UI;
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
        private readonly string _modelDir;

        internal AsrProfileFieldBinder(AsrProfile profile, AsrProjectSettings settings, string modelDir)
        {
            Profile = profile;
            _settings = settings;
            _modelDir = modelDir;
        }

        internal TextField BindText(string label, string value, AsrProfileField field)
        {
            var textField = new TextField(label) { value = value };
            var handler = new TextHandler(Profile, _settings, field);
            textField.RegisterValueChangedCallback(handler.Handle);
            return textField;
        }

        internal ModelObjectField BindFile(string label, string value, AsrProfileField field,
            string extension = "onnx", string keyword = "", bool isRequired = false)
        {
            var picker = new ModelObjectField(label, value, _modelDir, extension, keyword,
                isRequired: isRequired);
            var handler = new TextHandler(Profile, _settings, field);
            picker.RegisterFileChangedCallback(handler.SetValue);
            return picker;
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

            internal void SetValue(string value)
            {
                AsrProfileFieldSetter.SetString(_p, _f, value);
                _s.SaveSettings();
            }

            internal void Handle(ChangeEvent<string> evt) => SetValue(evt.newValue);
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
