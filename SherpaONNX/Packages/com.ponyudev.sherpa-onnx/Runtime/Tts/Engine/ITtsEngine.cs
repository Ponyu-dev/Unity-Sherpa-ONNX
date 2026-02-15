using System;
using PonyuDev.SherpaOnnx.Tts.Data;

namespace PonyuDev.SherpaOnnx.Tts.Engine
{
    /// <summary>
    /// Abstraction over the native TTS engine.
    /// Allows mocking for tests and swapping implementations.
    /// </summary>
    public interface ITtsEngine : IDisposable
    {
        int SampleRate { get; }
        int NumSpeakers { get; }
        bool IsLoaded { get; }

        void Load(TtsProfile profile, string modelDir);
        TtsResult Generate(string text, float speed, int speakerId);
        void Unload();
    }
}
