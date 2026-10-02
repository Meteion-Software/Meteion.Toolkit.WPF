using System.Windows;
using Meteion.Toolkit.WPF.Hosting;
using Microsoft.Extensions.Logging;

namespace Meteion.Toolkit.WPF.SplashScreen;

/// <summary>Takes ownership of closing the splash on behalf of the host.</summary>
internal sealed class SplashScreenLifecycleHook : IWpfHostLifecycleHook
{
    private readonly ISplashScreen _splash;

    /// <summary>Creates the hook and routes the splash's logging through the host.</summary>
    /// <param name="splash">The splash registered with the host.</param>
    /// <param name="logger">The host logger the splash should use from now on.</param>
    public SplashScreenLifecycleHook(ISplashScreen splash, ILogger<ISplashScreen> logger)
    {
        _splash = splash;

        // From here on the splash logs through the host's logging, and anything buffered so far is flushed to it.
        if (splash is SplashScreenHandle handle)
        {
            handle.AttachLogger(logger);
        }
    }

    /// <inheritdoc />
    public void OnLaunchWindowCreated(Window launchWindow) => _ = _splash.CloseWhenRendered(launchWindow);

    /// <inheritdoc />
    public void OnStartupFailed(Exception exception) => _splash.Dispose();
}
