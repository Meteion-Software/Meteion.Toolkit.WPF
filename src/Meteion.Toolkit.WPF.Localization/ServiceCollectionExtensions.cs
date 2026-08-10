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

        if (configure is not null)
        {
            services.Configure<LocalizationOptions>(configure);
        }

        return services;
    }
}
