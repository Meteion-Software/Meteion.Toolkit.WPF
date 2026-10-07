using Meteion.Toolkit.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Meteion.Toolkit.WPF.Dialogs;

/// <summary>
/// Dependency injection registration for the dialog service.
/// </summary>
public static class DialogServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="DefaultFilesystemDialogService"/> as the singleton <see cref="IFilesystemDialogService"/>, unless one is already registered.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <returns>The same <paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddMeteionDialogs(this IServiceCollection services)
    {
        services.TryAddSingleton<IFilesystemDialogService, DefaultFilesystemDialogService>();
        return services;
    }
}
