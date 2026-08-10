using Meteion.Toolkit.WPF.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace Meteion.Toolkit.WPF.Localization;

/// <summary>
/// All this does is provide access to the instance of ILocalizationService.
/// </summary>
internal static class LocalizationServiceLocator
{
    // TODO: determine a better way to do this. Especially if they aren't using WpfGenericHostApplication.
    public static Func<IServiceProvider> ServiceProviderAccessor { get; set; } = () => ((WpfGenericHostApplication)Application.Current).Host.Services;

    public static T Resolve<T>() where T : notnull => ServiceProviderAccessor().GetRequiredService<T>();
}
