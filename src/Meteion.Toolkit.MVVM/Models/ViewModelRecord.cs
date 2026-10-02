using Microsoft.Extensions.DependencyInjection;

namespace Meteion.Toolkit.MVVM.Models;

/// <summary>
/// Pairs a view model with the view type that displays it and the DI lifetime both are registered with.
/// </summary>
/// <param name="PageType">The view (page or window) type associated with the view model.</param>
/// <param name="Lifetime">The service lifetime used when registering the view model and view.</param>
public record ViewModelRecord(Type PageType, ServiceLifetime Lifetime);

