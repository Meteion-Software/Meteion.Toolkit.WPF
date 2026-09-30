using System.IO;
using System.Diagnostics;
using Meteion.Toolkit.WPF.SplashScreen.Native;
using Microsoft.Extensions.Logging;

namespace Meteion.Toolkit.WPF.SplashScreen;

/// <summary>A consistent copy of everything the splash thread needs to know, taken under the handle's lock.</summary>
internal readonly record struct SplashSignals(double Progress, bool Indeterminate, string? Status, bool Handoff, bool Close, bool Dispose);

/// <summary>
/// The caller-facing half of the splash: holds the latest requested state and posts a single coalesced message to
/// the splash thread whenever it changes. No member blocks on the splash thread. The thread-affine half (window,
/// GDI+ objects, animation) is <see cref="SplashWindow"/>.
/// </summary>
internal sealed class SplashScreenHandle : ISplashScreen
{
    /// <summary>Posted to the splash window whenever <see cref="TakeSignals"/> has something new to return.</summary>
    internal const uint WM_SIGNAL = NativeMethods.WM_APP + 1;

    private readonly object _gate = new();
    private readonly TaskCompletionSource _exited = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly long _shownAt = Stopwatch.GetTimestamp();

    private nint _hwnd;
    private bool _signalPending;
    private bool _failed;
    private bool _handoffRequested;
    private bool _closeRequested;
    private bool _disposeRequested;
    private double _progress;
    private bool _indeterminate;
    private string? _status;

    private SplashScreenHandle(SplashScreenOptions options, Func<Stream> openImage, SplashLog log)
    {
        Options = options;
        OpenImage = openImage;
        Log = log;
        _status = options.ShowStatusText ? options.InitialStatusText : null;
    }

    internal SplashScreenOptions Options { get; }

    internal Func<Stream> OpenImage { get; }

    internal SplashLog Log { get; }

    /// <summary>Time since <see cref="SplashScreenBuilder.Show"/>, for <see cref="SplashScreenOptions.MinimumDisplayTime"/>.</summary>
    internal TimeSpan ElapsedSinceShown => Stopwatch.GetElapsedTime(_shownAt);

    /// <summary>True once the splash failed after configuration validation; it is then a silent no-op.</summary>
    internal bool IsFailed
    {
        get
        {
            lock (_gate)
            {
                return _failed;
            }
        }
    }

    /// <summary>Completes when the splash thread has exited (for any reason).</summary>
    internal Task Exited => _exited.Task;

    /// <summary>Starts the splash thread and returns immediately.</summary>
    internal static SplashScreenHandle Start(SplashScreenOptions options, Func<Stream> openImage, SplashLog log)
    {
        var handle = new SplashScreenHandle(options, openImage, log);
        try
        {
            var thread = new Thread(handle.ThreadMain)
            {
                Name = "Meteion.SplashScreen",
                IsBackground = true, // can never keep the process alive
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }
        catch (Exception ex)
        {
            handle.Fail(ex);
            handle._exited.TrySetResult();
        }

        return handle;
    }

    /// <summary>Attaches the logger the host resolved and flushes any buffered failures to it.</summary>
    internal void AttachLogger(ILogger logger) => Log.Attach(logger);

    // ---- ISplashScreen ----

    public void SetProgress(double value) => Report(SplashProgress.Determinate(value));

    public void SetIndeterminate() => Report(SplashProgress.Indeterminate());

    public void SetStatus(string text) => Report(SplashProgress.Status(text));

    public void Report(double value, string text) => Report(SplashProgress.Determinate(value, text));

    public void Report(SplashProgress value)
    {
        lock (_gate)
        {
            if (_failed || _closeRequested || _disposeRequested)
            {
                return;
            }

            var changed = false;

            if (Options.ShowProgressBar)
            {
                switch (value.Bar)
                {
                    case SplashProgress.BarChange.Determinate when !double.IsNaN(value.Value):
                        _progress = Math.Clamp(value.Value, 0, 1);
                        _indeterminate = false;
                        changed = true;
                        break;
                    case SplashProgress.BarChange.Indeterminate:
                        _indeterminate = true;
                        changed = true;
                        break;
                }
            }

            if (value.StatusText is not null && Options.ShowStatusText)
            {
                _status = value.StatusText;
                changed = true;
            }

            if (changed)
            {
                Signal();
            }
        }
    }

    public Task Close()
    {
        lock (_gate)
        {
            if (_failed)
            {
                return Task.CompletedTask;
            }

            if (!_closeRequested)
            {
                _closeRequested = true;
                Signal();
            }
        }

        return _exited.Task;
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposeRequested)
            {
                return;
            }

            _disposeRequested = true;
            _closeRequested = true;
            Signal();
        }
    }

    // ---- Cross-thread plumbing ----

    /// <summary>Makes the splash topmost and click-through so the main window is usable beneath it.</summary>
    internal void BeginHandoff()
    {
        lock (_gate)
        {
            if (_failed || _handoffRequested)
            {
                return;
            }

            _handoffRequested = true;
            Signal();
        }
    }

    /// <summary>Called by the splash thread once its window exists. Anything requested earlier is picked up by <see cref="TakeSignals"/>.</summary>
    internal void AttachWindow(nint hwnd)
    {
        lock (_gate)
        {
            _hwnd = hwnd;
        }
    }

    internal void DetachWindow()
    {
        lock (_gate)
        {
            _hwnd = 0;
        }
    }

    /// <summary>Returns the latest state and re-arms the coalescing flag so the next change posts a new message.</summary>
    internal SplashSignals TakeSignals()
    {
        lock (_gate)
        {
            _signalPending = false;
            return new SplashSignals(_progress, _indeterminate, _status, _handoffRequested, _closeRequested, _disposeRequested);
        }
    }

    /// <summary>Records a failure: logs it and turns the handle into a no-op. Safe from any thread.</summary>
    internal void Fail(Exception exception)
    {
        lock (_gate)
        {
            _failed = true;
        }

        Log.Error("The splash screen failed and has been disabled; the application will continue without it.", exception);
    }

    // Must be called with _gate held. A burst of reports collapses into one posted message.
    private void Signal()
    {
        if (_hwnd != 0 && !_signalPending)
        {
            _signalPending = NativeMethods.PostMessageW(_hwnd, WM_SIGNAL, 0, 0);
        }
    }

    private void ThreadMain()
    {
        try
        {
            // Thread-level only: the process-wide DPI awareness (and so WPF and the app manifest) is never touched.
            NativeMethods.SetThreadDpiAwarenessContext(NativeMethods.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
            using var window = new SplashWindow(this);
            window.Run();
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
        finally
        {
            DetachWindow();
            _exited.TrySetResult();
        }
    }
}
