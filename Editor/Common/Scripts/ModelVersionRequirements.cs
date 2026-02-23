using System;
using System.Collections.Generic;
using PonyuDev.SherpaOnnx.Asr.Offline.Data;
using PonyuDev.SherpaOnnx.Asr.Online.Data;
using PonyuDev.SherpaOnnx.Tts.Data;
using PonyuDev.SherpaOnnx.Vad.Data;

namespace PonyuDev.SherpaOnnx.Editor.Common
{
    /// <summary>
    /// Maps every model type to the minimum sherpa-onnx version that introduced it.
    /// All enum values must have an explicit entry — unknown types are treated
    /// as unsupported until registered.
    /// </summary>
    internal static class ModelVersionRequirements
    {
        // ── Per-module version maps ──

        private static readonly Dictionary<TtsModelType, string> TtsVersions = new()
        {
            { TtsModelType.Vits,     "1.9.30"  },
            { TtsModelType.Matcha,   "1.10.16" },
            { TtsModelType.Kokoro,   "1.10.40" },
            { TtsModelType.Kitten,   "1.12.8"  },
            { TtsModelType.ZipVoice, "1.12.11" },
            { TtsModelType.Pocket,   "1.12.24" },
        };

        private static readonly Dictionary<AsrModelType, string> AsrVersions = new()
        {
            { AsrModelType.Transducer,   "1.10.0"  },
            { AsrModelType.Paraformer,   "1.10.29" },
            { AsrModelType.Whisper,      "1.10.0"  },
            { AsrModelType.SenseVoice,   "1.10.17" },
            { AsrModelType.Moonshine,    "1.10.30" },
            { AsrModelType.NemoCtc,      "1.10.13" },
            { AsrModelType.ZipformerCtc, "1.10.0"  },
            { AsrModelType.Tdnn,         "1.10.0"  },
            { AsrModelType.FireRedAsr,   "1.10.45" },
            { AsrModelType.Dolphin,      "1.11.3"  },
            { AsrModelType.Canary,       "1.12.5"  },
            { AsrModelType.WenetCtc,     "1.12.12" },
            { AsrModelType.Omnilingual,  "1.12.16" },
            { AsrModelType.MedAsr,       "1.12.21" },
            { AsrModelType.FunAsrNano,   "1.12.21" },
        };

        private static readonly Dictionary<OnlineAsrModelType, string> OnlineAsrVersions = new()
        {
            { OnlineAsrModelType.Transducer,    "1.10.0"  },
            { OnlineAsrModelType.Paraformer,    "1.10.29" },
            { OnlineAsrModelType.Zipformer2Ctc, "1.10.0"  },
            { OnlineAsrModelType.NemoCtc,       "1.10.8"  },
            { OnlineAsrModelType.ToneCtc,       "1.10.0"  },
        };

        private static readonly Dictionary<VadModelType, string> VadVersions = new()
        {
            { VadModelType.SileroVad, "1.9.30" },
            { VadModelType.TenVad,    "1.12.6" },
        };

        // ── Public API ──

        internal static string GetMinVersion(TtsModelType type)
            => Lookup(type, TtsVersions);

        internal static string GetMinVersion(AsrModelType type)
            => Lookup(type, AsrVersions);

        internal static string GetMinVersion(OnlineAsrModelType type)
            => Lookup(type, OnlineAsrVersions);

        internal static string GetMinVersion(VadModelType type)
            => Lookup(type, VadVersions);

        internal static bool IsSupported(TtsModelType type, string installedVersion)
            => Check(type, TtsVersions, installedVersion);

        internal static bool IsSupported(AsrModelType type, string installedVersion)
            => Check(type, AsrVersions, installedVersion);

        internal static bool IsSupported(OnlineAsrModelType type, string installedVersion)
            => Check(type, OnlineAsrVersions, installedVersion);

        internal static bool IsSupported(VadModelType type, string installedVersion)
            => Check(type, VadVersions, installedVersion);

        // ── Testable core ──

        internal static string Lookup<T>(T type, Dictionary<T, string> map)
        {
            return map.TryGetValue(type, out string v) ? v : null;
        }

        internal static bool Check<T>(
            T type, Dictionary<T, string> map, string installedVersion)
        {
            string min = Lookup(type, map);

            if (string.IsNullOrEmpty(min))
                return false;

            if (!Version.TryParse(installedVersion, out Version installed))
                return false;

            if (!Version.TryParse(min, out Version required))
                return false;

            return installed >= required;
        }
    }
}
