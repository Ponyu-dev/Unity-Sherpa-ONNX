using System;
using System.IO;
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
                    "iOS libraries (DLL + xcframeworks) are downloaded as a single archive "
                    + "from PonyuDev/Unity-Sherpa-ONNX releases. The extracted archive is "
                    + "cached so each configuration can be applied without re-downloading. "
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

            _cleanButton = new Button(HandleClean) { text = "Clean cache" };
            foldout.Add(_cleanButton);

            _openButton = new Button(HandleOpen) { text = "Open cache" };
            foldout.Add(_openButton);

            RefreshButtons();
            _cache.OnCacheChanged += HandleCacheChanged;
        }

        public void Dispose()
        {
            _cache.OnCacheChanged -= HandleCacheChanged;
            _cleanButton = null;
            _openButton = null;
        }

        private void HandleCacheChanged() => RefreshButtons();

        private void RefreshButtons()
        {
            bool exists = Directory.Exists(_cache.CachePath);
            _cleanButton?.SetEnabled(exists);
            _openButton?.SetEnabled(exists);
        }

        private void HandleClean() => _cache.Clean();

        private void HandleOpen() =>
            EditorUtility.RevealInFinder(_cache.CachePath);
    }
}
