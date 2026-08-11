using Meteion.Toolkit.Localization.Abstractions;
using Meteion.Toolkit.WPF.Localization.Resolution;
using Meteion.Toolkit.WPF.Localization.Tests.Fakes;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Meteion.Toolkit.WPF.Localization.Tests.Resolution;

public class ResourceAssemblyResolverTests
{
    private static readonly Assembly ExplicitAssembly = typeof(ResourceAssemblyResolverTests).Assembly;
    private static readonly Assembly XamlInferredAssembly = typeof(ResourceAssemblyResolver).Assembly;
    private static readonly Assembly DefaultAssembly = typeof(object).Assembly;

    private static ResourceAssemblyResolver CreateResolver(Assembly? xamlResult, Assembly? defaultAssembly)
        => new(new FakeXamlAssemblyResolver(xamlResult),
               Options.Create(new LocalizationOptions { DefaultAssembly = defaultAssembly }));

    [Fact]
    public void Resolve_ExplicitAssembly_WinsOverEverythingElse()
    {
        var resolver = CreateResolver(xamlResult: XamlInferredAssembly, defaultAssembly: DefaultAssembly);

        var resolved = resolver.Resolve(ExplicitAssembly, new FakeProvideValueServiceProvider());

        Assert.Same(ExplicitAssembly, resolved);
    }

    [Fact]
    public void Resolve_NoExplicit_UsesXamlInferredAssembly()
    {
        var resolver = CreateResolver(xamlResult: XamlInferredAssembly, defaultAssembly: DefaultAssembly);

        var resolved = resolver.Resolve(null, new FakeProvideValueServiceProvider());

        Assert.Same(XamlInferredAssembly, resolved);
    }

    [Fact]
    public void Resolve_NoExplicitAndXamlUnresolvable_FallsBackToDefaultAssembly()
    {
        var resolver = CreateResolver(xamlResult: null, defaultAssembly: DefaultAssembly);

        var resolved = resolver.Resolve(null, new FakeProvideValueServiceProvider());

        Assert.Same(DefaultAssembly, resolved);
    }

    [Fact]
    public void Resolve_NothingResolvable_ThrowsLocalizationConfigurationException()
    {
        var resolver = CreateResolver(xamlResult: null, defaultAssembly: null);

        Assert.Throws<LocalizationConfigurationException>(
            () => resolver.Resolve(null, new FakeProvideValueServiceProvider()));
    }
}
