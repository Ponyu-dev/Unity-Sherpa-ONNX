using System.Collections.Generic;

namespace PonyuDev.SherpaOnnx.Editor.LibraryInstall.Helpers
{
    internal static class LibraryPlatforms
    {
        /// <summary>
        /// URL for the iOS-specific managed DLL (with __Internal binding).
        /// Downloaded as sherpa-onnx.zip containing sherpa-onnx.dll.
        /// Tag format: sherpa-v{version}
        /// </summary>
        public const string IosManagedDllUrl =
            "https://github.com/Ponyu-dev/Unity-Sherpa-ONNX/releases/download/sherpa-v{0}/sherpa-onnx.zip";

        public static readonly LibraryArch ManagedLibrary =
            new()
            {
                Name = "Managed .dll",
                Url = "https://www.nuget.org/api/v2/package/org.k2fsa.sherpa.onnx/{0}",
                RootPath = ""
            };
        
        public static readonly List<LibraryPlatform> Platforms = new()
        {
            new LibraryPlatform
            {
                PlatformName = "Windows",
                Arches = new List<LibraryArch>()
                {
                    new()
                    {
                        Name = "win-x64",
                        Url = "https://www.nuget.org/api/v2/package/org.k2fsa.sherpa.onnx.runtime.win-x64/{0}",
                        RootPath = "",
                        IsManagedDllRoot = true,
                    },
                    new()
                    {
                        Name = "win-x86",
                        Url = "https://www.nuget.org/api/v2/package/org.k2fsa.sherpa.onnx.runtime.win-x86/{0}",
                        RootPath = "",
                        IsManagedDllRoot = true,
                    },
                    new()
                    {
                        Name = "win-arm64",
                        Url = "https://www.nuget.org/api/v2/package/org.k2fsa.sherpa.onnx.runtime.win-arm64/{0}",
                        RootPath = "",
                        IsManagedDllRoot = true,
                    }
                }
            },
            
            new LibraryPlatform
            {
                PlatformName = "Mac OS",
                Arches = new List<LibraryArch>()
                {
                    new()
                    {
                        Name = "osx-arm64",
                        Url = "https://www.nuget.org/api/v2/package/org.k2fsa.sherpa.onnx.runtime.osx-arm64/{0}",
                        RootPath = "",
                        IsManagedDllRoot = true,
                    },
                    new()
                    {
                        Name = "osx-x64",
                        Url = "https://www.nuget.org/api/v2/package/org.k2fsa.sherpa.onnx.runtime.osx-x64/{0}",
                        RootPath = "",
                        IsManagedDllRoot = true,
                    }
                }
            },
            
            new LibraryPlatform
            {
                PlatformName = "Linux",
                Arches = new List<LibraryArch>()
                {
                    new()
                    {
                        Name = "linux-arm64",
                        Url = "https://www.nuget.org/api/v2/package/org.k2fsa.sherpa.onnx.runtime.linux-arm64/{0}",
                        RootPath = "",
                        IsManagedDllRoot = true,
                    },
                    new()
                    {
                        Name = "linux-x64",
                        Url = "https://www.nuget.org/api/v2/package/org.k2fsa.sherpa.onnx.runtime.linux-x64/{0}",
                        RootPath = "",
                        IsManagedDllRoot = true,
                    }
                }
            },
            
            new LibraryPlatform
            {
                PlatformName = "Android",
                Arches = new List<LibraryArch>()
                {
                    new()
                    {
                        Name = "arm64-v8a",
                        Url = "https://github.com/k2-fsa/sherpa-onnx/releases/download/v{0}/sherpa-onnx-v{1}-android.tar.bz2",
                        RootPath = "",
                        IsManagedDllRoot = true,
                        Platform = PlatformType.Android,
                    },
                    new()
                    {
                        Name = "armeabi-v7a",
                        Url = "https://github.com/k2-fsa/sherpa-onnx/releases/download/v{0}/sherpa-onnx-v{1}-android.tar.bz2",
                        RootPath = "",
                        IsManagedDllRoot = true,
                        Platform = PlatformType.Android,
                    },
                    new()
                    {
                        Name = "x86",
                        Url = "https://github.com/k2-fsa/sherpa-onnx/releases/download/v{0}/sherpa-onnx-v{1}-android.tar.bz2",
                        RootPath = "",
                        IsManagedDllRoot = true,
                        Platform = PlatformType.Android,
                    },
                    new()
                    {
                        Name = "x86_64",
                        Url = "https://github.com/k2-fsa/sherpa-onnx/releases/download/v{0}/sherpa-onnx-v{1}-android.tar.bz2",
                        RootPath = "",
                        IsManagedDllRoot = true,
                        Platform = PlatformType.Android,
                    }
                }
            },
            
            new LibraryPlatform
            {
                PlatformName = "iOS",
                Arches = new List<LibraryArch>()
                {
                    new()
                    {
                        Name = "arm64",
                        Url = "https://github.com/k2-fsa/sherpa-onnx/releases/download/v{0}/sherpa-onnx-v{1}-ios.tar.bz2",
                        RootPath = "",
                        IsManagedDllRoot = false,
                        Platform = PlatformType.iOS,
                    },
                    new()
                    {
                        Name = "x86_64-simulator",
                        Url = "https://github.com/k2-fsa/sherpa-onnx/releases/download/v{0}/sherpa-onnx-v{1}-ios.tar.bz2",
                        RootPath = "",
                        IsManagedDllRoot = false,
                        Platform = PlatformType.iOS,
                    },
                }
            }
        };
    }
    
    internal class LibraryPlatform
    {
        public string PlatformName;
        public List<LibraryArch> Arches = new();
    }
    
    internal enum PlatformType
    {
        Desktop,
        Android,
        iOS
    }

    internal class LibraryArch
    {
        public string Name;
        public string Url;
        public string RootPath;
        public bool IsManagedDllRoot;
        public PlatformType Platform = PlatformType.Desktop;
    }
}