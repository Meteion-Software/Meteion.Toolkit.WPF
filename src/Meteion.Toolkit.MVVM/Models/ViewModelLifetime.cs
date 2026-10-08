using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Meteion.Toolkit.MVVM.Models;

/// <summary>
/// Resolves the service lifetime a view model is registered with.
/// </summary>
internal static class ViewModelLifetime
{
    /// <summary>
    /// Resolves a lifetime using the precedence: explicit value, then <see cref="ViewModelOptionsAttribute"/> on the
    /// view model type, then the supplied default.
    /// </summary>
    /// <param name="viewModelType">The view model type to inspect for the attribute.</param>
    /// <param name="explicitLifetime">A lifetime passed by the caller, which wins when not null.</param>
    /// <param name="defaultLifetime">The lifetime used when there is neither an explicit value nor an attribute.</param>
    /// <returns>The resolved lifetime.</returns>
    public static ServiceLifetime Resolve(Type viewModelType, ServiceLifetime? explicitLifetime, ServiceLifetime defaultLifetime = ServiceLifetime.Scoped)
    {
        return explicitLifetime
            ?? viewModelType.GetCustomAttribute<ViewModelOptionsAttribute>(inherit: false)?.Lifetime
            ?? defaultLifetime;
    }
}
