using Meteion.Toolkit.Localization.Abstractions;
using Meteion.Toolkit.WPF.Localization.Resolution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Meteion.Toolkit.WPF.Localization.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddWpfLocalization_RegistersAllResolutionAndLocalizationServices()
    {
        var services = new ServiceCollection();
        services.AddWpfLocalization(null);
        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IXamlAssemblyResolver>());
        Assert.NotNull(provider.GetRequiredService<IResourceAssemblyResolver>());
        Assert.NotNull(provider.GetRequiredService<ILocalizationProvider>());
        Assert.NotNull(provider.GetRequiredService<ILocalizationService>());
    }

    [Fact]
    public void AddWpfLocalization_LocalizationServiceIsASingleton()
    {
        var services = new ServiceCollection();
        services.AddWpfLocalization(null);
        var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<ILocalizationService>();
        var second = provider.GetRequiredService<ILocalizationService>();

        Assert.Same(first, second);
    }

    [Fact]
    public void AddWpfLocalization_ConfigureCallback_IsApplied()
    {
        var services = new ServiceCollection();
        var defaultAssembly = typeof(ServiceCollectionExtensionsTests).Assembly;
        services.AddWpfLocalization(options => options.DefaultAssembly = defaultAssembly);
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<LocalizationOptions>>().Value;

        Assert.Same(defaultAssembly, options.DefaultAssembly);
    }

    [Fact]
    public void AddWpfLocalization_NoConfigureCallback_ResolvesWithDefaultOptions()
    {
        var services = new ServiceCollection();
        services.AddWpfLocalization(null);
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<LocalizationOptions>>().Value;

        Assert.Null(options.DefaultAssembly);
    }
}
