using Meteion.Toolkit.Localization.Abstractions;
using Meteion.Toolkit.WPF.Localization.Resolution;
using Microsoft.Extensions.DependencyInjection;

namespace Meteion.Toolkit.WPF.Localization;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWpfLocalization(this IServiceCollection services, Action<LocalizationOptions>? configure)
    {
        services.AddSingleton<IResourceAssemblyResolver, ResourceAssemblyResolver>();
        services.AddSingleton<ILocalizationProvider, ResxLocalizationProvider>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<IXamlAssemblyResolver,  XamlAssemblyResolver>();

        // AddOptions() unconditionally, so IOptions<LocalizationOptions> resolves with
        // its defaults even when the caller doesn't pass a configure callback at all —
        // Configure<T> alone only registers IOptions<T> when it's actually called.
        services.AddOptions<LocalizationOptions>();

        if (configure is not null)
        {
            services.Configure(configure);
        }

        return services;
    }
}
