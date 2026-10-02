using Meteion.Toolkit.WPF.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Meteion.Toolkit.WPF.SplashScreen;

/// <summary>Integrates a splash screen with <see cref="HostApplicationBuilder"/> and the WPF host.</summary>
public static class SplashScreenHostingExtensions
{
    /// <summary>
    /// Recommended. Registers a splash that was shown <em>before</em> the host builder existed, so it appears as early
    /// as possible. The host closes it once the launch window has rendered, or disposes it if startup fails.
    /// </summary>
    /// <param name="builder">The host builder to register the splash with.</param>
    /// <param name="splash">The already-shown splash, typically from <see cref="SplashScreenBuilder.Show"/>.</param>
    /// <returns><paramref name="builder"/>, for chaining.</returns>
    public static HostApplicationBuilder UseSplashScreen(this HostApplicationBuilder builder, ISplashScreen splash)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(splash);

        // The same handle under both service types, injectable into hosted services, the App constructor and view models.
        builder.Services.AddSingleton(splash);
        builder.Services.AddSingleton<IProgress<SplashProgress>>(splash);
        builder.Services.AddSingleton<IWpfHostLifecycleHook, SplashScreenLifecycleHook>();
        return builder;
    }

    /// <summary>
    /// Convenience. Builds and shows the splash immediately (after the host builder was constructed, so a little
    /// later than <see cref="UseSplashScreen(HostApplicationBuilder, ISplashScreen)"/>).
    /// </summary>
    /// <param name="builder">The host builder to register the splash with.</param>
    /// <param name="configure">Configures the splash (image, options, logger) before it is shown.</param>
    /// <returns><paramref name="builder"/>, for chaining.</returns>
    public static HostApplicationBuilder UseSplashScreen(this HostApplicationBuilder builder, Action<SplashScreenBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var splashBuilder = new SplashScreenBuilder();
        configure(splashBuilder);
        return builder.UseSplashScreen(splashBuilder.Show());
    }
}
