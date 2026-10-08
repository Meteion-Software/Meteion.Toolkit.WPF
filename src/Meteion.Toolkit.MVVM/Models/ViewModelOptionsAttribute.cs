using Microsoft.Extensions.DependencyInjection;

namespace Meteion.Toolkit.MVVM;

/// <summary>
/// Declares registration options for a view model, such as the service lifetime it is registered with when it is
/// mapped to a view. An explicit lifetime passed when mapping a view model always takes precedence over this attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ViewModelOptionsAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the service lifetime the view model and its view are registered with.
    /// Defaults to <see cref="ServiceLifetime.Scoped"/>.
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Scoped;
}
