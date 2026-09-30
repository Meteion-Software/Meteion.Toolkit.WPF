using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using Meteion.Toolkit.WPF.SplashScreen.Native;
using Meteion.Toolkit.WPF.SplashScreen.Rendering;

namespace Meteion.Toolkit.WPF.SplashScreen;

/// <summary>
/// The splash thread's half of the splash: a borderless layered window, its Win32 message pump, the cached
/// GDI+ renderer and all animation (fades, progress smoothing, the indeterminate segment). Everything here runs on
/// the one splash thread; <see cref="SplashScreenHandle"/> talks to it only through a posted message.
/// </summary>
internal sealed unsafe class SplashWindow : IDisposable
{
    private const nuint AnimationTimerId = 1;
    private const nuint HoldTimerId = 2;
    private const uint AnimationIntervalMs = 17; // ~60 fps cap
    private const double IndeterminateCycleMs = 1200;

    private readonly SplashScreenHandle _handle;
    private readonly SplashScreenOptions _options;

    private SplashRenderer? _renderer;
    private SplashCanvas? _canvas;
    private GCHandle _self;
    private string? _className;
    private nint _instance;
    private nint _hwnd;

    // Displayed state
    private double _target;
    private double _displayed;
    private double _smoothFrom;
    private long _smoothStart;
    private bool _smoothing;
    private bool _indeterminate;
    private string? _text;
    private readonly long _epoch = Stopwatch.GetTimestamp();

    // Window lifecycle
    private bool _fadingIn;
    private long _fadeInStart;
    private bool _fadingOut;
    private long _fadeOutStart;
    private bool _closing;
    private bool _handoffApplied;
    private bool _animationTimerRunning;

    public SplashWindow(SplashScreenHandle handle)
    {
        _handle = handle;
        _options = handle.Options;
    }

    /// <summary>Decodes the image, creates and shows the window, then pumps messages until it is destroyed.</summary>
    public void Run()
    {
        var scale = PrepareRenderer(out var workArea);
        if (_renderer is null || _handle.TakeSignals().Dispose)
        {
            return;
        }

        var size = _renderer.PixelSize;
        _canvas = new SplashCanvas(size.Width, size.Height);

        CreateWindow(workArea, size);
        _handle.AttachWindow(_hwnd);

        // Start from whatever was reported before the window existed, then draw the first frame.
        var initial = _handle.TakeSignals();
        _target = _displayed = initial.Progress;
        _indeterminate = initial.Indeterminate;
        _text = initial.Status;

        if (_options.FadeIn && _options.FadeInDuration > TimeSpan.Zero)
        {
            _fadingIn = true;
            _fadeInStart = Stopwatch.GetTimestamp();
        }

        Redraw(render: true);
        NativeMethods.ShowWindow(_hwnd, NativeMethods.SW_SHOWNOACTIVATE);

        // Pick up a handoff/close/dispose that was requested while we were starting.
        ProcessSignals();
        UpdateTimer();

        Pump();
    }

    public void Dispose()
    {
        if (_hwnd != 0)
        {
            NativeMethods.DestroyWindow(_hwnd);
            _hwnd = 0;
        }

        _canvas?.Dispose();
        _renderer?.Dispose();
        _canvas = null;
        _renderer = null;

        if (_className is not null)
        {
            NativeMethods.UnregisterClassW(_className, _instance);
            _className = null;
        }

        if (_self.IsAllocated)
        {
            _self.Free();
        }
    }

    // ---- Startup ----

    private float PrepareRenderer(out NativeMethods.RECT workArea)
    {
        // The monitor under the cursor, else the primary. This thread is per-monitor-DPI-aware, so all of these are physical pixels.
        NativeMethods.POINT cursor = default;
        NativeMethods.GetCursorPos(&cursor);
        var monitor = NativeMethods.MonitorFromPoint(cursor, NativeMethods.MONITOR_DEFAULTTOPRIMARY);

        var info = new NativeMethods.MONITORINFO { CbSize = (uint)sizeof(NativeMethods.MONITORINFO) };
        if (!NativeMethods.GetMonitorInfoW(monitor, &info))
        {
            throw new InvalidOperationException("GetMonitorInfo failed.");
        }

        workArea = info.RcWork;

        var scale = 1f;
        if (_options.DpiScaling)
        {
            uint dpiX = 96, dpiY = 96;
            if (NativeMethods.GetDpiForMonitor(monitor, NativeMethods.MDT_EFFECTIVE_DPI, &dpiX, &dpiY) == 0 && dpiX > 0)
            {
                scale = dpiX / 96f;
            }
        }

        using var stream = _handle.OpenImage();
        using var decoded = new Bitmap(stream); // the stream must outlive the bitmap's use
        if (_handle.TakeSignals().Dispose)
        {
            return scale;
        }

        _renderer = new SplashRenderer(decoded, _options, scale, _handle.Log);
        return scale;
    }

    private void CreateWindow(NativeMethods.RECT workArea, Size size)
    {
        _instance = NativeMethods.GetModuleHandleW(null);
        _className = "Meteion.SplashScreen." + Guid.NewGuid().ToString("N");
        _self = GCHandle.Alloc(this);

        fixed (char* className = _className)
        {
            var windowClass = new NativeMethods.WNDCLASSEXW
            {
                CbSize = (uint)sizeof(NativeMethods.WNDCLASSEXW),
                LpfnWndProc = (nint)(delegate* unmanaged[Stdcall]<nint, uint, nuint, nint, nint>)&WndProc,
                HInstance = _instance,
                HCursor = NativeMethods.LoadCursorW(0, NativeMethods.IDC_ARROW),
                LpszClassName = className,
            };

            if (NativeMethods.RegisterClassExW(&windowClass) == 0)
            {
                _className = null;
                throw new InvalidOperationException("RegisterClassEx failed.");
            }
        }

        var x = workArea.Left + (((workArea.Right - workArea.Left) - size.Width) / 2);
        var y = workArea.Top + (((workArea.Bottom - workArea.Top) - size.Height) / 2);

        // NOACTIVATE: the splash never takes focus. TOOLWINDOW: no taskbar button.
        var exStyle = NativeMethods.WS_EX_LAYERED | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE;
        if (_options.TopMost)
        {
            exStyle |= NativeMethods.WS_EX_TOPMOST;
        }

        _hwnd = NativeMethods.CreateWindowExW(
            exStyle, _className, null, NativeMethods.WS_POPUP, x, y, size.Width, size.Height, 0, 0, _instance, GCHandle.ToIntPtr(_self));
        if (_hwnd == 0)
        {
            throw new InvalidOperationException("CreateWindowEx failed.");
        }
    }

    private static void Pump()
    {
        NativeMethods.MSG msg;
        int result;
        while ((result = NativeMethods.GetMessageW(&msg, 0, 0, 0)) > 0)
        {
            NativeMethods.DispatchMessageW(&msg);
        }

        if (result < 0)
        {
            throw new InvalidOperationException("GetMessage failed.");
        }
    }

    // ---- Window procedure ----

    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvStdcall)])]
    private static nint WndProc(nint hwnd, uint msg, nuint wParam, nint lParam)
    {
        if (msg == NativeMethods.WM_NCCREATE)
        {
            // CREATESTRUCT.lpCreateParams is its first field: the GCHandle we passed to CreateWindowEx.
            NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWLP_USERDATA, *(nint*)lParam);
        }
        else
        {
            var state = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWLP_USERDATA);
            if (state != 0 && GCHandle.FromIntPtr(state).Target is SplashWindow window)
            {
                // An exception must never escape an UnmanagedCallersOnly method.
                try
                {
                    if (window.HandleMessage(msg, wParam))
                    {
                        return 0;
                    }
                }
                catch (Exception ex)
                {
                    window.OnFault(ex);
                    return 0;
                }
            }
        }

        return NativeMethods.DefWindowProcW(hwnd, msg, wParam, lParam);
    }

    private bool HandleMessage(uint msg, nuint wParam)
    {
        switch (msg)
        {
            case SplashScreenHandle.WM_SIGNAL:
                ProcessSignals();
                return true;

            case NativeMethods.WM_TIMER when wParam == AnimationTimerId:
                Tick();
                return true;

            case NativeMethods.WM_TIMER when wParam == HoldTimerId:
                NativeMethods.KillTimer(_hwnd, HoldTimerId);
                StartFadeOut();
                return true;

            case NativeMethods.WM_DESTROY:
                _hwnd = 0;
                _handle.DetachWindow();
                NativeMethods.PostQuitMessage(0);
                return true;

            default:
                return false;
        }
    }

    private void OnFault(Exception exception)
    {
        _handle.Fail(exception);
        Teardown();
    }

    // ---- State and closing ----

    private void ProcessSignals()
    {
        if (_hwnd == 0)
        {
            return;
        }

        var signals = _handle.TakeSignals();
        if (signals.Dispose)
        {
            Teardown();
            return;
        }

        var dirty = false;

        if (signals.Indeterminate != _indeterminate)
        {
            _indeterminate = signals.Indeterminate;
            dirty = true;
        }

        if (signals.Progress != _target)
        {
            _target = signals.Progress;
            if (_options.SmoothProgress && _options.ProgressSmoothingDuration > TimeSpan.Zero)
            {
                _smoothFrom = _displayed;
                _smoothStart = Stopwatch.GetTimestamp();
                _smoothing = true;
            }
            else
            {
                _displayed = _target;
                _smoothing = false;
            }

            dirty = true;
        }

        if (!string.Equals(signals.Status, _text, StringComparison.Ordinal))
        {
            _text = signals.Status;
            dirty = true;
        }

        if (signals.Handoff && !_handoffApplied)
        {
            ApplyHandoff();
        }

        if (signals.Close && !_closing)
        {
            BeginClose();
        }

        if (dirty)
        {
            Redraw(render: true);
        }

        UpdateTimer();
    }

    private void ApplyHandoff()
    {
        _handoffApplied = true;
        NativeMethods.SetWindowPos(
            _hwnd, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0, NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
        MakeClickThrough();
    }

    private void MakeClickThrough()
    {
        var style = NativeMethods.GetWindowLongPtr(_hwnd, NativeMethods.GWL_EXSTYLE);
        NativeMethods.SetWindowLongPtr(_hwnd, NativeMethods.GWL_EXSTYLE, style | (nint)NativeMethods.WS_EX_TRANSPARENT);
    }

    private void BeginClose()
    {
        _closing = true;
        MakeClickThrough();

        var remaining = _options.MinimumDisplayTime - _handle.ElapsedSinceShown;
        if (remaining > TimeSpan.Zero)
        {
            NativeMethods.SetTimer(_hwnd, HoldTimerId, (uint)Math.Ceiling(remaining.TotalMilliseconds), 0);
        }
        else
        {
            StartFadeOut();
        }
    }

    private void StartFadeOut()
    {
        if (_hwnd == 0)
        {
            return;
        }

        if (!_options.FadeOut || _options.FadeOutDuration <= TimeSpan.Zero)
        {
            Teardown();
            return;
        }

        _fadingOut = true;
        _fadeOutStart = Stopwatch.GetTimestamp();
        UpdateTimer();
    }

    private void Teardown()
    {
        if (_hwnd == 0)
        {
            return;
        }

        NativeMethods.KillTimer(_hwnd, AnimationTimerId);
        NativeMethods.KillTimer(_hwnd, HoldTimerId);
        _animationTimerRunning = false;
        NativeMethods.DestroyWindow(_hwnd); // WM_DESTROY clears _hwnd and quits the pump
    }

    // ---- Animation ----

    private void UpdateTimer()
    {
        if (_hwnd == 0)
        {
            return;
        }

        var needed = _fadingIn || _fadingOut || _smoothing || (_indeterminate && _options.ShowProgressBar);
        if (needed && !_animationTimerRunning)
        {
            NativeMethods.SetTimer(_hwnd, AnimationTimerId, AnimationIntervalMs, 0);
            _animationTimerRunning = true;
        }
        else if (!needed && _animationTimerRunning)
        {
            NativeMethods.KillTimer(_hwnd, AnimationTimerId);
            _animationTimerRunning = false;
        }
    }

    private void Tick()
    {
        var render = false;

        if (_smoothing)
        {
            var t = Progress(_smoothStart, _options.ProgressSmoothingDuration);
            _displayed = _smoothFrom + ((_target - _smoothFrom) * (1 - Math.Pow(1 - t, 3))); // ease-out cubic
            if (t >= 1)
            {
                _displayed = _target;
                _smoothing = false;
            }

            render = true;
        }

        if (_indeterminate && _options.ShowProgressBar)
        {
            render = true;
        }

        if (_fadingIn && Progress(_fadeInStart, _options.FadeInDuration) >= 1)
        {
            _fadingIn = false;
        }

        if (_fadingOut && Progress(_fadeOutStart, _options.FadeOutDuration) >= 1)
        {
            Teardown();
            return;
        }

        Redraw(render);
        UpdateTimer();
    }

    private static double Progress(long start, TimeSpan duration) =>
        Math.Clamp(Stopwatch.GetElapsedTime(start).TotalMilliseconds / duration.TotalMilliseconds, 0, 1);

    private byte CurrentAlpha()
    {
        var alpha = 1.0;
        if (_fadingIn)
        {
            alpha *= Progress(_fadeInStart, _options.FadeInDuration);
        }

        if (_fadingOut)
        {
            alpha *= 1 - Progress(_fadeOutStart, _options.FadeOutDuration);
        }

        return (byte)Math.Round(255 * alpha);
    }

    // Re-renders the bar/text only when something visible changed; a pure fade just re-pushes the cached frame with a new alpha.
    private void Redraw(bool render)
    {
        if (_renderer is null || _canvas is null || _hwnd == 0)
        {
            return;
        }

        if (render)
        {
            var elapsedMs = Stopwatch.GetElapsedTime(_epoch).TotalMilliseconds;
            var phase = (elapsedMs % IndeterminateCycleMs) / IndeterminateCycleMs;
            _renderer.Render(_canvas, new SplashRenderState(_displayed, _indeterminate, phase, _text));
        }

        var size = new NativeMethods.SIZE { Cx = _canvas.Width, Cy = _canvas.Height };
        var source = default(NativeMethods.POINT);
        var blend = new NativeMethods.BLENDFUNCTION
        {
            BlendOp = NativeMethods.AC_SRC_OVER,
            SourceConstantAlpha = CurrentAlpha(),
            AlphaFormat = NativeMethods.AC_SRC_ALPHA,
        };

        if (!NativeMethods.UpdateLayeredWindow(_hwnd, 0, null, &size, _canvas.Hdc, &source, 0, &blend, NativeMethods.ULW_ALPHA))
        {
            throw new InvalidOperationException("UpdateLayeredWindow failed.");
        }
    }
}
