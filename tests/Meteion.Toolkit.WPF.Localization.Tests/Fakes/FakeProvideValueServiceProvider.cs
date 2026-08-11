using System.Windows.Markup;

namespace Meteion.Toolkit.WPF.Localization.Tests.Fakes;

/// <summary>
/// Minimal fake of the IServiceProvider that XAML's markup extension parsing passes into
/// ProvideValue. Lets tests configure IUriContext/IProvideValueTarget (or leave them
/// unregistered, mirroring "this service isn't available in this parsing context") without
/// any real WPF application or XAML parser running.
/// </summary>
public sealed class FakeProvideValueServiceProvider : IServiceProvider
{
    private readonly Dictionary<Type, object?> _services = new();

    public FakeProvideValueServiceProvider WithUriContext(Uri? baseUri)
    {
        _services[typeof(IUriContext)] = new FakeUriContext(baseUri);
        return this;
    }

    public FakeProvideValueServiceProvider WithProvideValueTarget(object? targetObject, object? targetProperty)
    {
        _services[typeof(IProvideValueTarget)] = new FakeProvideValueTarget(targetObject, targetProperty);
        return this;
    }

    public object? GetService(Type serviceType)
        => _services.TryGetValue(serviceType, out var service) ? service : null;

    private sealed class FakeUriContext(Uri? baseUri) : IUriContext
    {
        public Uri? BaseUri { get; set; } = baseUri;
    }

    private sealed class FakeProvideValueTarget(object? targetObject, object? targetProperty) : IProvideValueTarget
    {
        public object? TargetObject { get; } = targetObject;
        public object? TargetProperty { get; } = targetProperty;
    }
}
