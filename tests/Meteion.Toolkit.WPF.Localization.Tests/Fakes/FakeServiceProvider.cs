namespace Meteion.Toolkit.WPF.Localization.Tests.Fakes;

/// <summary>
/// Minimal generic IServiceProvider fake for standing in as
/// LocalizationServiceLocator.ServiceProviderAccessor's target — resolves whatever's
/// been registered by type, via Microsoft.Extensions.DependencyInjection's
/// GetRequiredService&lt;T&gt; extension method, without needing a real DI container.
/// </summary>
public sealed class FakeServiceProvider : IServiceProvider
{
    private readonly Dictionary<Type, object> _services = new();

    public FakeServiceProvider Add<T>(T service) where T : notnull
    {
        _services[typeof(T)] = service;
        return this;
    }

    public object? GetService(Type serviceType)
        => _services.TryGetValue(serviceType, out var service) ? service : null;
}
