using Meteion.Toolkit.Localization.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace Meteion.Toolkit.WPF.Localization;

/// <summary>
/// All this does is provide access to the instance of ILocalizationService.
/// </summary>
internal static class LocalizationServiceLocator
{
    /// <summary>
    /// Defaults to the running application's services. Throws a clear error when there is no
    /// <see cref="IServiceProviderApplication"/> - e.g. in the Visual Studio designer, where
    /// <see cref="Application.Current"/> is the designer's own surface app.
    /// </summary>
    public static Func<IServiceProvider> ServiceProviderAccessor { get; set; } = () =>
        (Application.Current as IServiceProviderApplication)?.Services
        ?? throw new LocalizationConfigurationException(
            $"Localization needs the application's services, but {Application.Current?.GetType().FullName ?? "no Application"} " +
            $"doesn't implement {nameof(IServiceProviderApplication)}. Derive from WpfGenericHostApplication, " +
            $"or implement {nameof(IServiceProviderApplication)} on your Application.");

    /// <summary>Gets a required service from the application's service provider.</summary>
    /// <typeparam name="T">The service type to resolve.</typeparam>
    /// <returns>The resolved service.</returns>
    public static T Resolve<T>() where T : notnull => ServiceProviderAccessor().GetRequiredService<T>();

    /// <summary>Gets an optional service from the application's service provider.</summary>
    /// <typeparam name="T">The service type to resolve.</typeparam>
    /// <returns>The resolved service, or <see langword="null"/> when it isn't registered.</returns>
    public static T? TryResolve<T>() where T : class => ServiceProviderAccessor().GetService<T>();
}
