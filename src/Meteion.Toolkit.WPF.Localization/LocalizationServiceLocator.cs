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

    public static T Resolve<T>() where T : notnull => ServiceProviderAccessor().GetRequiredService<T>();

    public static T? TryResolve<T>() where T : class => ServiceProviderAccessor().GetService<T>();
}
