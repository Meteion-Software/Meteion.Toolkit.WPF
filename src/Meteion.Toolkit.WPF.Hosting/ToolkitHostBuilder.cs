using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Windows;

namespace Meteion.Toolkit.WPF.Hosting;

internal class WpfHostOptions
{
    public Type? StartupWindowType { get; set; }
    public Type? ApplicationType { get; set; }
}

public static class HostBuilderExtensions
{
    // HostApplicationBuilder (modern API)
    public static HostApplicationBuilder ConfigureLaunchWindow<TWindow>(this HostApplicationBuilder builder)
        where TWindow : Window
    {
        builder.Services.Configure<WpfHostOptions>(o => o.StartupWindowType = typeof(TWindow));
        builder.Services.AddScoped<TWindow>();
        return builder;
    }

    public static HostApplicationBuilder ConfigureApplication<TApp>(this HostApplicationBuilder builder)
        where TApp : WpfGenericHostApplication
    {
        builder.Services.Configure<WpfHostOptions>(o => o.ApplicationType = typeof(TApp));
        builder.Services.AddSingleton<TApp>();
        return builder;
    }

    public static WpfApplicationHost BuildWpfHost(this HostApplicationBuilder builder, ILoggerFactory? loggerFactory = null)
    {
        var host = builder.Build();
        var options = host.Services.GetRequiredService<IOptions<WpfHostOptions>>().Value;
        if (options.StartupWindowType == null) throw new InvalidOperationException(nameof(ConfigureLaunchWindow) + " was not called.");
        if (options.ApplicationType == null) throw new InvalidOperationException(nameof(ConfigureApplication) + " was not called.");
        return new WpfApplicationHost(host, options.StartupWindowType, options.ApplicationType, loggerFactory?.CreateLogger<WpfApplicationHost>());
    }

    // IHostBuilder (legacy API)
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