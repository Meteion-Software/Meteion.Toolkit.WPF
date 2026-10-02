using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Windows;

namespace Meteion.Toolkit.WPF.Hosting;

/// <summary>
/// Holds the window and application types registered by <c>ConfigureLaunchWindow</c> and
/// <c>ConfigureApplication</c> so <c>BuildWpfHost</c> can read them back from the built host.
/// </summary>
internal class WpfHostOptions
{
    public Type? StartupWindowType { get; set; }
    public Type? ApplicationType { get; set; }
}

/// <summary>
/// Extension methods that register the WPF launch window and application type on a generic host builder and build a
/// <see cref="WpfApplicationHost"/>. Both <see cref="HostApplicationBuilder"/> and <see cref="IHostBuilder"/> are supported.
/// </summary>
public static class HostBuilderExtensions
{
    // HostApplicationBuilder (modern API)

    /// <summary>
    /// Registers the window that is shown when the application starts.
    /// </summary>
    /// <typeparam name="TWindow">The launch window type. It is resolved from a new DI scope at startup.</typeparam>
    /// <param name="builder">The host application builder to configure.</param>
    /// <returns>The same <paramref name="builder"/>, for chaining.</returns>
    public static HostApplicationBuilder ConfigureLaunchWindow<TWindow>(this HostApplicationBuilder builder)
        where TWindow : Window
    {
        builder.Services.Configure<WpfHostOptions>(o => o.StartupWindowType = typeof(TWindow));
        builder.Services.AddScoped<TWindow>();
        return builder;
    }

    /// <summary>
    /// Registers the WPF application class that the host runs.
    /// </summary>
    /// <typeparam name="TApp">The application type, registered as a singleton.</typeparam>
    /// <param name="builder">The host application builder to configure.</param>
    /// <returns>The same <paramref name="builder"/>, for chaining.</returns>
    public static HostApplicationBuilder ConfigureApplication<TApp>(this HostApplicationBuilder builder)
        where TApp : WpfGenericHostApplication
    {
        builder.Services.Configure<WpfHostOptions>(o => o.ApplicationType = typeof(TApp));
        builder.Services.AddSingleton<TApp>();
        return builder;
    }

    /// <summary>
    /// Builds the underlying host and wraps it in a <see cref="WpfApplicationHost"/>.
    /// </summary>
    /// <param name="builder">The host application builder to build.</param>
    /// <param name="loggerFactory">Optional factory used to create the host's logger.</param>
    /// <returns>A host that runs the configured WPF application when started.</returns>
    /// <exception cref="InvalidOperationException">
    /// <c>ConfigureLaunchWindow</c> or <c>ConfigureApplication</c> was not called.
    /// </exception>
    public static WpfApplicationHost BuildWpfHost(this HostApplicationBuilder builder, ILoggerFactory? loggerFactory = null)
    {
        var host = builder.Build();
        var options = host.Services.GetRequiredService<IOptions<WpfHostOptions>>().Value;
        if (options.StartupWindowType == null) throw new InvalidOperationException(nameof(ConfigureLaunchWindow) + " was not called.");
        if (options.ApplicationType == null) throw new InvalidOperationException(nameof(ConfigureApplication) + " was not called.");
        return new WpfApplicationHost(host, options.StartupWindowType, options.ApplicationType, loggerFactory?.CreateLogger<WpfApplicationHost>());
    }

    // IHostBuilder (legacy API)

    /// <inheritdoc cref="ConfigureLaunchWindow{TWindow}(HostApplicationBuilder)" />
    /// <param name="builder">The host builder to configure.</param>
    public static IHostBuilder ConfigureLaunchWindow<TWindow>(this IHostBuilder builder)
        where TWindow : Window
    {
        builder.ConfigureServices((ctx, services) =>
        {
            services.Configure<WpfHostOptions>(o => o.StartupWindowType = typeof(TWindow));
            services.AddScoped<TWindow>();
        });
        return builder;
    }

    /// <inheritdoc cref="ConfigureApplication{TApp}(HostApplicationBuilder)" />
    /// <param name="builder">The host builder to configure.</param>
    public static IHostBuilder ConfigureApplication<TApp>(this IHostBuilder builder)
        where TApp : WpfGenericHostApplication
    {
        builder.ConfigureServices((ctx, services) =>
        {
            services.Configure<WpfHostOptions>(o => o.ApplicationType = typeof(TApp));
            services.AddSingleton<TApp>();
        });
        return builder;
    }

    /// <inheritdoc cref="BuildWpfHost(HostApplicationBuilder, ILoggerFactory?)" />
    /// <param name="builder">The host builder to build.</param>
    /// <param name="loggerFactory">Optional factory used to create the host's logger.</param>
    public static WpfApplicationHost BuildWpfHost(this IHostBuilder builder, ILoggerFactory? loggerFactory = null)
    {
        // Read configured options from the built host's service provider
        var host = builder.Build();
        var options = host.Services.GetRequiredService<IOptions<WpfHostOptions>>().Value;
        if (options.StartupWindowType == null) throw new InvalidOperationException(nameof(ConfigureLaunchWindow) + " was not called.");
        if (options.ApplicationType == null) throw new InvalidOperationException(nameof(ConfigureApplication) + " was not called.");
        return new WpfApplicationHost(host, options.StartupWindowType, options.ApplicationType, loggerFactory?.CreateLogger<WpfApplicationHost>());
    }
}