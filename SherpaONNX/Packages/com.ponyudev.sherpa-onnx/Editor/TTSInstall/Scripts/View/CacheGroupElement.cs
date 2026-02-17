using PonyuDev.SherpaOnnx.Editor.TtsInstall.Settings;
using PonyuDev.SherpaOnnx.Tts.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.TtsInstall.View
{
    /// <summary>
    /// A single cache group: toggle, description, and pool size field.
    /// Binds to <see cref="TtsCacheSettings"/> via <see cref="CacheField"/> enum.
    /// </summary>
    internal sealed class CacheGroupElement : VisualElement
    {
        private readonly TtsCacheSettings _cache;
        private readonly TtsProjectSettings _settings;
        private readonly CacheField _field;
        private readonly IntegerField _sizeField;

        internal CacheGroupElement(
            string title,
            string description,
            bool enabled,
            int poolSize,
            TtsCacheSettings cache,
            TtsProjectSettings settings,
            CacheField field)
        {
            _cache = cache;
            _settings = settings;
            _field = field;

            AddToClassList("tts-cache-group");

            var toggle = new Toggle(title) { value = enabled };
            Add(toggle);

            var desc = new Label(description);
            desc.AddToClassList("tts-cache-description");
            Add(desc);

            _sizeField = new IntegerField("Pool size") { value = poolSize };
            _sizeField.AddToClassList("tts-cache-size-field");
            _sizeField.style.display = enabled
                ? DisplayStyle.Flex : DisplayStyle.None;
            _sizeField.RegisterValueChangedCallback(HandlePoolSizeChanged);
            Add(_sizeField);

            toggle.RegisterValueChangedCallback(HandleToggleChanged);
        }

        private void HandleToggleChanged(ChangeEvent<bool> evt)
        {
            switch (_field)
            {
                case CacheField.OfflineTts:
                    _cache.offlineTtsEnabled = evt.newValue;
                    break;
                case CacheField.ResultCache:
                    _cache.resultCacheEnabled = evt.newValue;
                    break;
                case CacheField.AudioClip:
                    _cache.audioClipEnabled = evt.newValue;
                    break;
                case CacheField.AudioSource:
                    _cache.audioSourceEnabled = evt.newValue;
                    break;
            }

            _settings.SaveSettings();
            _sizeField.style.display = evt.newValue
                ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void HandlePoolSizeChanged(ChangeEvent<int> evt)
        {
            switch (_field)
            {
                case CacheField.OfflineTts:
                    _cache.offlineTtsPoolSize = evt.newValue;
                    break;
                case CacheField.ResultCache:
                    _cache.resultCacheSize = evt.newValue;
                    break;
                case CacheField.AudioClip:
                    _cache.audioClipPoolSize = evt.newValue;
                    break;
                case CacheField.AudioSource:
                    _cache.audioSourcePoolSize = evt.newValue;
                    break;
            }

            _settings.SaveSettings();
        }
    }
}
