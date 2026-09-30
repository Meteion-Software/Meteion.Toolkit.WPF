namespace Meteion.Toolkit.WPF.SplashScreen;

/// <summary>
/// A handle to a running splash screen, used to report progress and to close it. All members are thread-safe and
/// never block the caller. Reports made after closing has begun, for a hidden element (bar or text disabled), or
/// on a splash that failed to start are silently ignored.
/// </summary>
public interface ISplashScreen : IProgress<SplashProgress>, IDisposable
{
    /// <summary>Sets the bar to <paramref name="value"/> (0.0–1.0, clamped).</summary>
    void SetProgress(double value);

    /// <summary>Switches the bar to indeterminate mode.</summary>
    void SetIndeterminate();

    /// <summary>Sets the status text. An empty string clears it.</summary>
    void SetStatus(string text);

    /// <summary>Sets the bar and the status text together.</summary>
    void Report(double value, string text);

    /// <summary>
    /// Gracefully closes: honors <see cref="SplashScreenOptions.MinimumDisplayTime"/>, then fades out. Non-blocking;
    /// a second call is a no-op returning the same task. The task completes when the splash thread has exited.
    /// </summary>
    Task Close();
}
