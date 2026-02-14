using System;
using PonyuDev.SherpaOnnx.Editor.LibraryInstall.Helpers;
using UnityEditor;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.LibraryInstall
{
    /// <summary>
    /// Reusable UI section that shows HelpBox, "Clean cache" and "Open cache"
    /// buttons for a platform archive cache.
    /// </summary>
    internal sealed class CacheSectionUi : IDisposable
    {
        /// <summary>
        /// Creates a <see cref="CacheSectionUi"/> for the given platform name,
        /// or null if the platform has no cache.
        /// </summary>
        internal static CacheSectionUi CreateForPlatform(string platformName)
        {
            if (platformName == "Android")
            {
                return new CacheSectionUi(
                    AndroidArchiveCache.Cache,
                    "Android libraries are downloaded as a single archive containing all "
                    + "architectures. The extracted archive is cached so each architecture "
                    + "can be installed without re-downloading. "
                    + "Use 'Clean cache' to remove the cached archive.");
            }

            if (platformName == "iOS")
            {
                return new CacheSectionUi(
                    iOSArchiveCache.Cache,
                    "iOS libraries are downloaded as a single archive containing both device "
                    + "and simulator frameworks. The extracted archive is cached so each "
                    + "architecture can be installed without re-downloading. "
                    + "Use 'Clean cache' to remove the cached archive.");
            }

            return null;
        }

        private readonly IArchiveCache _cache;
        private readonly string _helpText;

        private Button _cleanButton;
        private Button _openButton;

        internal CacheSectionUi(IArchiveCache cache, string helpText)
        {
            _cache = cache;
            _helpText = helpText;
        }

        internal void Build(Foldout foldout)
        {
            var helpBox = new HelpBox(_helpText, HelpBoxMessageType.Info);
            foldout.Add(helpBox);

            bool ready = _cache.IsReady;

            _cleanButton = new Button(HandleClean) { text = "Clean cache" };
            _cleanButton.SetEnabled(ready);
            foldout.Add(_cleanButton);

            _openButton = new Button(HandleOpen) { text = "Open cache" };
            _openButton.SetEnabled(ready);
            foldout.Add(_openButton);

            _cache.OnCacheChanged += HandleCacheChanged;
        }

        public void Dispose()
        {
            _cache.OnCacheChanged -= HandleCacheChanged;
            _cleanButton = null;
            _openButton = null;
        }

        private void HandleCacheChanged()
        {
            bool ready = _cache.IsReady;
            _cleanButton?.SetEnabled(ready);
            _openButton?.SetEnabled(ready);
        }

        private void HandleClean() => _cache.Clean();

        private void HandleOpen() =>
            EditorUtility.RevealInFinder(_cache.CachePath);
    }
}
