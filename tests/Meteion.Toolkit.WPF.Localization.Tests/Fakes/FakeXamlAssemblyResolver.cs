using Meteion.Toolkit.WPF.Localization.Resolution;
using System.Reflection;

namespace Meteion.Toolkit.WPF.Localization.Tests.Fakes;

/// <summary>
/// Fake IXamlAssemblyResolver that always returns whatever was configured, regardless
/// of the IServiceProvider passed in — lets ResourceAssemblyResolverTests exercise the
/// precedence chain without needing a real XAML parsing context.
/// </summary>
public sealed class FakeXamlAssemblyResolver(Assembly? result) : IXamlAssemblyResolver
{
    public Assembly? Resolve(IServiceProvider serviceProvider) => result;
}
