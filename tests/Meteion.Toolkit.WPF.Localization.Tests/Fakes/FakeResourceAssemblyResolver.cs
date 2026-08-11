using Meteion.Toolkit.Localization.Abstractions;
using System.Reflection;

namespace Meteion.Toolkit.WPF.Localization.Tests.Fakes;

/// <summary>
/// Fake IResourceAssemblyResolver that always returns a configured assembly and
/// records the explicit-assembly argument it was called with.
/// </summary>
public sealed class FakeResourceAssemblyResolver(Assembly result) : IResourceAssemblyResolver
{
    public Assembly? LastExplicitAssembly { get; private set; }

    public Assembly Resolve(Assembly? explicitAssembly, IServiceProvider provideValueServiceProvider)
    {
        LastExplicitAssembly = explicitAssembly;
        return result;
    }
}
