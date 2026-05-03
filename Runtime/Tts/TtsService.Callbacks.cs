using System.Threading;
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

        // ── Async callback generation (cancellable) ──

        /// <inheritdoc />
        public async Task<TtsResult> GenerateWithCallbackAsync(
            string text, float speed, int speakerId, TtsCallback callback,
            CancellationToken ct = default)
        {
            if (!CheckReady())
                return null;

            var engine = _engine;
            if (engine == null)
                return null;

            using var linked = LinkCt(ct);
            return await engine.GenerateWithCallbackAsync(
                text, speed, speakerId, callback, linked.Token);
        }

        /// <inheritdoc />
        public async Task<TtsResult> GenerateWithCallbackProgressAsync(
            string text, float speed, int speakerId,
            TtsCallbackProgress callback,
            CancellationToken ct = default)
        {
            if (!CheckReady())
                return null;

            var engine = _engine;
            if (engine == null)
                return null;

            using var linked = LinkCt(ct);
            return await engine.GenerateWithCallbackProgressAsync(
                text, speed, speakerId, callback, linked.Token);
        }

        /// <inheritdoc />
        public async Task<TtsResult> GenerateWithConfigAsync(
            string text, TtsGenerationConfig config,
            TtsCallbackProgress callback,
            CancellationToken ct = default)
        {
            if (!CheckReady())
                return null;

            var engine = _engine;
            if (engine == null)
                return null;

            using var linked = LinkCt(ct);
            return await engine.GenerateWithConfigAsync(
                text, config, callback, linked.Token);
        }
    }
}
