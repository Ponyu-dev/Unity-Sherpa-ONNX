using PonyuDev.SherpaOnnx.Asr.Offline.Data;
using PonyuDev.SherpaOnnx.Asr.Offline.Engine;

namespace PonyuDev.SherpaOnnx.Tests.Stubs
{
    /// <summary>
    /// Minimal <see cref="IAsrEngine"/> stub for unit tests.
    /// Tracks call counts and stores last arguments for assertion.
    /// </summary>
    public sealed class StubAsrEngine : IAsrEngine
    {
        public bool IsLoaded { get; set; }
        public int PoolSize { get; set; } = 1;

        // ── Call counters ──

        public int LoadCallCount { get; private set; }
        public int RecognizeCallCount { get; private set; }
        public int UnloadCallCount { get; private set; }
        public int ResizeCallCount { get; private set; }

        // ── Last arguments ──

        public AsrProfile LastProfile { get; private set; }
        public string LastModelDir { get; private set; }
        public int LastPoolSize { get; private set; }

        // ── Configurable output ──

        public AsrResult ResultToReturn { get; set; }

        // ── State ──

        public bool Disposed { get; private set; }

        // ── IAsrEngine ──

        public void Load(AsrProfile profile, string modelDir, int poolSize = 1)
        {
            LoadCallCount++;
            LastProfile = profile;
            LastModelDir = modelDir;
            LastPoolSize = poolSize;
            PoolSize = poolSize;
            IsLoaded = true;
        }

        public void Resize(int newPoolSize)
        {
            ResizeCallCount++;
            PoolSize = newPoolSize;
        }

        public AsrResult Recognize(float[] samples, int sampleRate)
        {
            RecognizeCallCount++;
            return ResultToReturn;
        }

        public void Unload()
        {
            UnloadCallCount++;
            IsLoaded = false;
        }

        public void Dispose()
        {
            Disposed = true;
            Unload();
        }
    }
}
