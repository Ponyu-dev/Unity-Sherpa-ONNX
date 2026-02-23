using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Tts.Data;
using PonyuDev.SherpaOnnx.Tts.Engine;

namespace PonyuDev.SherpaOnnx.Tts
{
    /// <summary>
    /// Callback-based generation methods for <see cref="TtsService"/>.
    /// </summary>
    public sealed partial class TtsService
    {
        // ── Sync callback generation ──

        /// <inheritdoc />
        public TtsResult GenerateWithCallback(
            string text, float speed, int speakerId, TtsCallback callback)
        {
            if (!CheckReady())
                return null;

            return _engine.GenerateWithCallback(
                text, speed, speakerId, callback);
        }

        /// <inheritdoc />
        public TtsResult GenerateWithCallbackProgress(
            string text, float speed, int speakerId,
            TtsCallbackProgress callback)
        {
            if (!CheckReady())
                return null;

            return _engine.GenerateWithCallbackProgress(
                text, speed, speakerId, callback);
        }

        /// <inheritdoc />
        public TtsResult GenerateWithConfig(
            string text, TtsGenerationConfig config,
            TtsCallbackProgress callback)
        {
            if (!CheckReady())
                return null;

            return _engine.GenerateWithConfig(text, config, callback);
        }

        // ── Async callback generation ──

        /// <inheritdoc />
        public Task<TtsResult> GenerateWithCallbackAsync(
            string text, float speed, int speakerId, TtsCallback callback)
        {
            if (!CheckReady())
                return Task.FromResult<TtsResult>(null);

            var engine = _engine;
            if (engine == null)
                return Task.FromResult<TtsResult>(null);

            return Task.Run(
                () => engine.GenerateWithCallback(
                    text, speed, speakerId, callback));
        }

        /// <inheritdoc />
        public Task<TtsResult> GenerateWithCallbackProgressAsync(
            string text, float speed, int speakerId,
            TtsCallbackProgress callback)
        {
            if (!CheckReady())
                return Task.FromResult<TtsResult>(null);

            var engine = _engine;
            if (engine == null)
                return Task.FromResult<TtsResult>(null);

            return Task.Run(
                () => engine.GenerateWithCallbackProgress(
                    text, speed, speakerId, callback));
        }

        /// <inheritdoc />
        public Task<TtsResult> GenerateWithConfigAsync(
            string text, TtsGenerationConfig config,
            TtsCallbackProgress callback)
        {
            if (!CheckReady())
                return Task.FromResult<TtsResult>(null);

            var engine = _engine;
            if (engine == null)
                return Task.FromResult<TtsResult>(null);

            return Task.Run(
                () => engine.GenerateWithConfig(text, config, callback));
        }
    }
}
