# Splash Screen

`Meteion.Toolkit.WPF.SplashScreen` is an optional package that shows a native splash screen (an image, plus an
optional progress bar and status text) from `Program.Main`, **before the WPF runtime starts**, and hands off to your
launch window once it has rendered.

- Appears as early as possible and never slows your startup: decoding, window creation and drawing all happen on a
  dedicated background thread.
- Honors the PNG's per-pixel alpha, DPI-scaled and crisp.
- Works with or without `Meteion.Toolkit.WPF.Hosting`.
- Never crashes the app: anything that fails after your configuration validates degrades to "no splash" and is logged.
- .NET 8+ only. No WinForms, no COM interop.

## Usage with the host (recommended)

```cs
[STAThread]
public static void Main(string[] args)
{
    // Show the splash FIRST, before the host builder exists.
    var splash = new SplashScreenBuilder()
        .UseImageFromEmbeddedResource("Splash.png")
        .Configure(o => { o.ShowProgressBar = true; o.ShowStatusText = true; })
        .Show();

    var builder = new HostApplicationBuilder()
        .ConfigureLaunchWindow<MainWindow>()
        .ConfigureApplication<App>()
        .UseSplashScreen(splash);

    builder.BuildWpfHost().Run();
}
```

**Why before `new HostApplicationBuilder()`?** Constructing the builder loads configuration, logging and
a good deal of the runtime. `Show()` only validates and starts a thread, so calling it first puts pixels on screen
as early as possible. `UseSplashScreen(Action<SplashScreenBuilder>)` is a convenience that shows the splash at that
point instead, which is a little later.

`UseSplashScreen` registers `ISplashScreen` and `IProgress<SplashProgress>` as singletons (the same handle), so any
hosted service, the `App` constructor or a view model can report progress. Hosted services run before the window
appears, which makes them a natural place to report:

```cs
public class WarmupService(IProgress<SplashProgress> progress) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        progress.Report(SplashProgress.Determinate(0.2, "Opening database…"));
        // ...
        progress.Report(SplashProgress.Indeterminate("Contacting server…"));
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
```

The host takes ownership of closing: once the launch window's `ContentRendered` fires it does the handoff below, and if
startup throws it disposes the splash so nothing is left orphaned.

## Standalone usage

Without the host you own the handle. Call `CloseWhenRendered(window)`, `Close()` or `Dispose()` yourself:

```cs
var splash = new SplashScreenBuilder().UseImageFromFilesystem("Assets/Splash.png").Show();
// ... later, once your window exists:
await splash.CloseWhenRendered(mainWindow);
```

| Method | Waits for window render | Min display time | Fade-out | Returns |
| --- | --- | --- | --- | --- |
| `CloseWhenRendered(window)` | Yes, and activates it | Honored | Yes | `Task`, completes when the splash thread exits |
| `Close()` | No | Honored | Yes | `Task`, completes when the splash thread exits |
| `Dispose()` | No | Skipped | Skipped | Immediate teardown |

All members are thread-safe and non-blocking. Reports made after closing began, for a hidden element, or on a failed
splash are silently ignored.

## Images

> **The image must have the `EmbeddedResource` build action.** WPF `Resource` items (pack URIs) are not supported.

```xml
<ItemGroup>
  <EmbeddedResource Include="Assets\Splash.png" />
</ItemGroup>
```

- `UseImageFromEmbeddedResource(name)` searches the entry assembly (or the `Assembly` you pass): exact manifest name
  first, then a `.{name}` suffix match. An ambiguous suffix or no match throws from `Show()` and lists the candidates
  or every resource in the assembly, which makes a wrong build action easy to spot.
- `UseImageFromFilesystem(path)` resolves relative paths against `AppContext.BaseDirectory`, never the current
  directory. A missing file throws `FileNotFoundException` with the resolved path.
- **Transparency:** the PNG's alpha is always honored. If you want a solid background, bake it into the image.

## Options

**All sizes, locations and font sizes are in _image pixels_ relative to the image's top-left corner — not points or
DIPs.** `FontSize = 14` means 14 pixels tall on the image, not 14 pt. With `DpiScaling` (the default) the whole
composite is scaled by the monitor's DPI.

| Option | Default |
| --- | --- |
| `ShowProgressBar` | `false` |
| `ProgressBarLocation` / `ProgressBarSize` | `null` (auto: 60% of image width × 6 px, centered, bottom edge 8% of image height above the bottom) |
| `ProgressBarFillColor` / `ProgressBarTrackColor` | `#0078D4` / `#40FFFFFF` |
| `ProgressBarCornerRadius` | `0` |
| `SmoothProgress` / `ProgressSmoothingDuration` | `true` / 150 ms |
| `ShowStatusText` | `false` |
| `StatusTextRegion` | `null` (auto: one line directly above the bar, or where the bar would be if hidden) |
| `FontFamily` / `FontSize` / `FontStyle` | Segoe UI / 14 px / Regular |
| `TextColor` | White |
| `TextHorizontalAlignment` / `TextVerticalAlignment` | Center / Center |
| `InitialStatusText` | `null` |
| `TopMost` | `false` |
| `FadeIn` / `FadeInDuration` | `false` / 150 ms |
| `FadeOut` / `FadeOutDuration` | `true` / 200 ms |
| `MinimumDisplayTime` | `TimeSpan.Zero` (measured from `Show()`) |
| `DpiScaling` | `true` |

Text is drawn without wrapping and with an ellipsis when it is too long for the region. A missing font family falls
back to Segoe UI and logs a warning. The indeterminate segment (25% of the bar, 1.2 s cycle) and the 60 fps animation
cap are fixed in v1.

`Configure` can be called more than once; the delegates run in call order.

## Reporting progress

`SplashProgress` is created only through factories, so each report says what to change:

| Factory | Bar | Text |
| --- | --- | --- |
| `Determinate(0.4)` | set to 0.4 | unchanged |
| `Determinate(0.4, "Loading…")` | set to 0.4 | set |
| `Indeterminate()` | switch to indeterminate | unchanged |
| `Indeterminate("Connecting…")` | switch to indeterminate | set |
| `Status("Almost there…")` | unchanged | set |

`null` status = unchanged; `""` clears the text. `ISplashScreen` also has `SetProgress`, `SetIndeterminate`,
`SetStatus` and `Report(double, string)` shorthands.

## Handoff

1. The launch window's `ContentRendered` fires.
2. The splash becomes topmost and click-through, so the main window is usable immediately.
3. The main window is activated underneath.
4. The splash holds until `MinimumDisplayTime` (from `Show()`) has elapsed, if any remains.
5. It fades out (or closes instantly if `FadeOut = false`) and its thread exits.

`MinimumDisplayTime` only delays the splash disappearing, never the main window. If the user alt-tabs away during the
final hold or fade, the splash briefly stays over the other app.

## Errors and logging

Configuration mistakes (no image, `Show()` twice, unresolved image, invalid option values) throw from `Show()`.
Anything later (decoding, window creation, rendering) is logged and turns the handle into a silent no-op.

Without a logger, failures are buffered and written to `System.Diagnostics.Trace`. Call `UseLogger(ILogger)` if you have
one early; with host integration the `ILogger<ISplashScreen>` from DI is attached automatically and the buffer is
flushed to it.

## How it works

The splash is a borderless, non-interactive layered window (`WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE`)
centered in the work area of the monitor under the cursor, with no child controls. GDI+ renders a premultiplied
bitmap that is pushed with `UpdateLayeredWindow`. The scaled background is rendered once; each update redraws only the
bar and text. The splash thread opts into per-monitor-v2 DPI awareness for itself only; the process-wide setting is
untouched.

## Host extensibility: `IWpfHostLifecycleHook`

The splash integrates through a small, splash-agnostic hook added to `Meteion.Toolkit.WPF.Hosting`. Register any
number of `IWpfHostLifecycleHook` services and `WpfApplicationHost.StartAsync` will call
`OnLaunchWindowCreated(Window)` (UI thread, just before `Application.Run`) or `OnStartupFailed(Exception)` (before
the host rethrows). A hook that throws is logged and never masks the original exception.
