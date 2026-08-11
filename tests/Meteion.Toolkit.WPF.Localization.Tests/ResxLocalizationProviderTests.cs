using Meteion.Toolkit.Localization.Abstractions;
using Meteion.Toolkit.WPF.Localization.Tests.Fixtures.MultipleResx;
using Meteion.Toolkit.WPF.Localization.Tests.Fixtures.NoResx;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Reflection;

namespace Meteion.Toolkit.WPF.Localization.Tests;

public class ResxLocalizationProviderTests
{
    private static readonly Assembly ThisTestAssembly = typeof(ResxLocalizationProviderTests).Assembly;

    private static ResxLocalizationProvider CreateProvider(LocalizationOptions? options = null)
        => new(Options.Create(options ?? new LocalizationOptions()));

    [Fact]
    public void GetLocalizedString_NeutralCulture_ReturnsValueFromResx()
    {
        var provider = CreateProvider();

        var value = provider.GetLocalizedString("Greeting", ThisTestAssembly, CultureInfo.InvariantCulture);

        Assert.Equal("Hello", value);
    }

    [Fact]
    public void GetLocalizedString_CultureWithSatelliteResx_ReturnsCultureSpecificValue()
    {
        var provider = CreateProvider();

        var value = provider.GetLocalizedString("Greeting", ThisTestAssembly, new CultureInfo("ja-JP"));

        Assert.Equal("Hello (ja-JP)", value);
    }

    [Fact]
    public void GetLocalizedString_UnknownKey_ReturnsNull()
    {
        var provider = CreateProvider();

        var value = provider.GetLocalizedString("DoesNotExist", ThisTestAssembly, CultureInfo.InvariantCulture);

        Assert.Null(value);
    }

    [Fact]
    public void GetAvailableKeys_ReturnsAllNeutralKeys()
    {
        var provider = CreateProvider();

        var keys = provider.GetAvailableKeys(ThisTestAssembly).ToHashSet();

        Assert.Equal(new HashSet<string> { "Greeting", "Farewell" }, keys);
    }

    [Fact]
    public void GetLocalizedString_AssemblyHasCompiledXamlResourcesToo_DoesNotTreatGDotResourcesAsAmbiguous()
    {
        // This test assembly has both TestStrings.resx AND a compiled DummyResources.xaml
        // (which produces a "*.g.resources" manifest entry). If the provider didn't
        // exclude ".g.resources", every test in this class would already be throwing
        // "ambiguous" — the fact that the tests above pass at all proves the exclusion
        // works, but this test asserts it explicitly and names why.
        var provider = CreateProvider();
        var names = ThisTestAssembly.GetManifestResourceNames();

        Assert.Contains(names, n => n.EndsWith(".g.resources", StringComparison.Ordinal));
        Assert.Equal("Hello", provider.GetLocalizedString("Greeting", ThisTestAssembly, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void GetLocalizedString_AssemblyWithNoResx_ThrowsLocalizationConfigurationException()
    {
        var provider = CreateProvider();

        var ex = Assert.Throws<LocalizationConfigurationException>(
            () => provider.GetLocalizedString("Anything", typeof(Meteion.Toolkit.WPF.Localization.Tests.Fixtures.NoResx.Marker).Assembly, CultureInfo.InvariantCulture));

        Assert.Contains("no embedded .resources files", ex.Message);
    }

    [Fact]
    public void GetLocalizedString_AssemblyWithMultipleUnrelatedResx_ThrowsLocalizationConfigurationException()
    {
        var provider = CreateProvider();
        var ambiguousAssembly = typeof(Meteion.Toolkit.WPF.Localization.Tests.Fixtures.MultipleResx.Marker).Assembly;

        var ex = Assert.Throws<LocalizationConfigurationException>(
            () => provider.GetLocalizedString("Sample", ambiguousAssembly, CultureInfo.InvariantCulture));

        Assert.Contains("multiple embedded .resources files", ex.Message);
    }

    [Fact]
    public void GetLocalizedString_ResourceBaseNameSelectorConfigured_BypassesAmbiguityCheck()
    {
        var ambiguousAssembly = typeof(Meteion.Toolkit.WPF.Localization.Tests.Fixtures.MultipleResx.Marker).Assembly;
        var provider = CreateProvider(new LocalizationOptions
        {
            ResourceBaseNameSelector = asm => $"{asm.GetName().Name}.First"
        });

        var value = provider.GetLocalizedString("Sample", ambiguousAssembly, CultureInfo.InvariantCulture);

        Assert.Equal("First", value);
    }
}
