using PonyuDev.SherpaOnnx.Editor.Common.UI;
using PonyuDev.SherpaOnnx.Editor.TtsInstall.Settings;
using PonyuDev.SherpaOnnx.Tts.Data;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.TtsInstall.Presenters
{
    /// <summary>
    /// Binds UI fields to <see cref="TtsProfile"/> properties via
    /// <see cref="ProfileField"/> enum — no lambdas required.
    /// </summary>
    internal sealed class ProfileFieldBinder
    {
        internal readonly TtsProfile Profile;
        private readonly TtsProjectSettings _settings;
        private readonly string _modelDir;

        internal ProfileFieldBinder(TtsProfile profile, TtsProjectSettings settings, string modelDir)
        {
            Profile = profile;
            _settings = settings;
            _modelDir = modelDir;
        }

        internal TextField BindText(string label, string value, ProfileField field)
        {
            var textField = new TextField(label) { value = value };
            var handler = new TextHandler(Profile, _settings, field);
            textField.RegisterValueChangedCallback(handler.Handle);
            return textField;
        }

        internal ModelObjectField BindFile(string label, string value, ProfileField field,
            string extension = "onnx", string keyword = "", bool isRequired = false)
        {
            var picker = new ModelObjectField(label, value, _modelDir, extension, keyword,
                isRequired: isRequired);
            var handler = new TextHandler(Profile, _settings, field);
            picker.RegisterFileChangedCallback(handler.SetValue);
            return picker;
        }

        internal ModelObjectField BindFolder(string label, string value, ProfileField field,
            string keyword = "")
        {
            var picker = new ModelObjectField(label, value, _modelDir, keyword: keyword, isFolder: true);
            var handler = new TextHandler(Profile, _settings, field);
            picker.RegisterFileChangedCallback(handler.SetValue);
            return picker;
        }

        internal FloatField BindFloat(string label, float value, ProfileField field)
        {
            var floatField = new FloatField(label) { value = value };
            var handler = new FloatHandler(Profile, _settings, field);
            floatField.RegisterValueChangedCallback(handler.Handle);
            return floatField;
        }

        internal IntegerField BindInt(string label, int value, ProfileField field)
        {
            var intField = new IntegerField(label) { value = value };
            var handler = new IntHandler(Profile, _settings, field);
            intField.RegisterValueChangedCallback(handler.Handle);
            return intField;
        }

        // ── Handlers ──

        private sealed class TextHandler
        {
            private readonly TtsProfile _p;
            private readonly TtsProjectSettings _s;
            private readonly ProfileField _f;

            internal TextHandler(TtsProfile p, TtsProjectSettings s, ProfileField f)
            { _p = p; _s = s; _f = f; }

            internal void SetValue(string value)
            {
                ProfileFieldSetter.SetString(_p, _f, value);
                _s.SaveSettings();
            }

            internal void Handle(ChangeEvent<string> evt) => SetValue(evt.newValue);
        }

        private sealed class FloatHandler
        {
            private readonly TtsProfile _p;
            private readonly TtsProjectSettings _s;
            private readonly ProfileField _f;

            internal FloatHandler(TtsProfile p, TtsProjectSettings s, ProfileField f)
            { _p = p; _s = s; _f = f; }

            internal void Handle(ChangeEvent<float> evt)
            {
                ProfileFieldSetter.SetFloat(_p, _f, evt.newValue);
                _s.SaveSettings();
            }
        }

        private sealed class IntHandler
        {
            private readonly TtsProfile _p;
            private readonly TtsProjectSettings _s;
            private readonly ProfileField _f;

            internal IntHandler(TtsProfile p, TtsProjectSettings s, ProfileField f)
            { _p = p; _s = s; _f = f; }

            internal void Handle(ChangeEvent<int> evt)
            {
                ProfileFieldSetter.SetInt(_p, _f, evt.newValue);
                _s.SaveSettings();
            }
        }
    }
}
