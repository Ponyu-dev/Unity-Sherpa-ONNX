using System.Threading;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Common.Platform
{
    /// <summary>
    /// Captures Unity's main-thread <see cref="SynchronizationContext"/>
    /// at runtime startup and exposes a <see cref="Post"/> helper for
    /// marshaling work back onto the main thread from any other
    /// context.
    ///
    /// Service init runs progress callbacks from whichever thread is
    /// pushing work at the time — file downloads, archive extraction,
    /// and the native engine ctor all run on the thread pool. UI
    /// consumers (UI Toolkit, IMGUI, classic UGUI) require those
    /// callbacks to land on the main thread, otherwise property
    /// setters throw <c>UnityException</c>. This dispatcher is the
    /// canonical place plugin code (or host code that sits on top of
    /// <c>Action&lt;ProfileReadyEvent&gt;</c>) routes through to keep
    /// thread-affinity right without each subscriber rolling its own
    /// capture.
    ///
    /// Captured once via <see cref="RuntimeInitializeOnLoadMethodAttribute"/>
    /// at <c>BeforeSceneLoad</c> — that runs on the main thread before
    /// any service kicks off <c>InitializeAsync</c>, so every Post
    /// site sees a non-null context.
    /// </summary>
    public static class MainThreadDispatcher
    {
        private static SynchronizationContext _context;
        private static int _mainThreadId;

        /// <summary>
        /// True when called on the same thread that captured the
        /// context — i.e. Unity's main thread. False from worker
        /// threads. Also returns true when the context has not been
        /// captured yet (very early boot, or non-Unity host like an
        /// EditMode test) so callers can fall back to a synchronous
        /// invocation rather than dropping the work on the floor.
        /// </summary>
        public static bool IsCurrent
        {
            get
            {
                if (_context == null)
                    return true;
                return Thread.CurrentThread.ManagedThreadId == _mainThreadId;
            }
        }

        /// <summary>
        /// Posts <paramref name="callback"/> with <paramref name="state"/>
        /// onto the main thread's sync-context. The callback runs on
        /// the next Unity frame. No-op if the context was never
        /// captured (caller should invoke synchronously in that case
        /// — see <see cref="IsCurrent"/>).
        /// </summary>
        public static void Post(SendOrPostCallback callback, object state)
        {
            if (_context == null)
                return;
            _context.Post(callback, state);
        }

        // BeforeSceneLoad runs on the main thread once per app start,
        // before any service kicks off InitializeAsync. Capturing here
        // means every bus / consumer is ready to marshal as soon as
        // they see their first event.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            _context = SynchronizationContext.Current;
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }
    }
}
