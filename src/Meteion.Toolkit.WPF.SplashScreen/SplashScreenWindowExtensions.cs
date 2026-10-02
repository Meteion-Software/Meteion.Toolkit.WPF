using System.Windows;

namespace Meteion.Toolkit.WPF.SplashScreen;

/// <summary>
/// WPF-typed extensions for <see cref="ISplashScreen"/>. They live in their own class so that loading
/// <see cref="ISplashScreen"/> never forces PresentationFramework to load.
/// </summary>
public static class SplashScreenWindowExtensions
{
    /// <summary>
    /// Hands off from the splash to <paramref name="window"/>: when the window's content has rendered the splash
    /// goes topmost and click-through, the window is activated underneath, and the splash then honors
    /// <see cref="SplashScreenOptions.MinimumDisplayTime"/>, fades out and closes. If the window closes without ever
    /// rendering, the splash is closed anyway.
    /// </summary>
    /// <param name="splash">The splash to hand off from.</param>
    /// <param name="window">The window that replaces the splash; typically the application's main window.</param>
    /// <returns>A task that completes when the splash thread has exited.</returns>
    public static Task CloseWhenRendered(this ISplashScreen splash, Window window)
    {
        ArgumentNullException.ThrowIfNull(splash);
        ArgumentNullException.ThrowIfNull(window);

        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        void Finish(bool rendered)
        {
            window.ContentRendered -= OnRendered;
            window.Closed -= OnClosed;

            if (rendered && splash is SplashScreenHandle handle)
            {
                handle.BeginHandoff();
            }

            if (rendered)
            {
                // The process owns the foreground, so this succeeds. The splash is already above the window by now.
                window.Activate();
            }

            splash.Close().ContinueWith(_ => done.TrySetResult(), TaskScheduler.Default);
        }

        void OnRendered(object? sender, EventArgs e) => Finish(rendered: true);
        void OnClosed(object? sender, EventArgs e) => Finish(rendered: false);

        window.ContentRendered += OnRendered;
        window.Closed += OnClosed;
        return done.Task;
    }
}
