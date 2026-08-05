using Microsoft.Extensions.DependencyInjection;

namespace Meteion.Toolkit.MVVM.Models;

/// <summary>
/// Contains a record for a ViewModels page type and it's lifetime.
/// </summary>
public record ViewModelRecord(Type PageType, ServiceLifetime Lifetime);

