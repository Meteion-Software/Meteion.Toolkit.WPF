using Meteion.Toolkit.Localization.Abstractions;
using Meteion.Toolkit.WPF.Localization.Tests.Fixtures.MultipleResx;
using Meteion.Toolkit.WPF.Localization.Tests.Fixtures.NoResx;
using System.Globalization;
using System.Reflection;

namespace Meteion.Toolkit.WPF.Localization.Tests;

public class ResxLocalizationProviderTests
{
    private static readonly Assembly ThisTestAssembly = typeof(ResxLocalizationProviderTests).Assembly;

    private static readonly Assembly MultipleResxAssembly = typeof(Meteion.Toolkit.WPF.Localization.Tests.Fixtures.MultipleResx.Marker).Assembly;

    private static ResxLocalizationProvider CreateProvider() => new();

    private static LocalizationKey Key(string key) => LocalizationKey.Unqualified(key);

    [Fact]
    public void GetLocalizedString_NeutralCulture_ReturnsValueFromResx()
    {
        var provider = CreateProvider();

        var value = provider.GetLocalizedString(Key("Greeting"), ThisTestAssembly, CultureInfo.InvariantCulture);

        Assert.Equal("Hello", value);
    }

    [Fact]
    public void GetLocalizedString_CultureWithSatelliteResx_ReturnsCultureSpecificValue()
    {
        var provider = CreateProvider();

        var value = provider.GetLocalizedString(Key("Greeting"), ThisTestAssembly, new CultureInfo("ja-JP"));

        Assert.Equal("Hello (ja-JP)", value);
    }

    [Fact]
    public void GetLocalizedString_UnknownKey_ReturnsNull()
    {
        var provider = CreateProvider();

        var value = provider.GetLocalizedString(Key("DoesNotExist"), ThisTestAssembly, CultureInfo.InvariantCulture);

        Assert.Null(value);
    }

    [Fact]
    public void GetAvailableKeys_ReturnsAllNeutralKeys()
    {
        var provider = CreateProvider();

        var keys = provider.GetAvailableKeys(ThisTestAssembly).ToHashSet();

        var baseName = $"{ThisTestAssembly.GetName().Name}.Resources.TestStrings";
        Assert.Equal(new HashSet<string>
        {
            $"{ThisTestAssembly.GetName().Name}/{baseName}:Greeting",
            $"{ThisTestAssembly.GetName().Name}/{baseName}:Farewell",
        }, keys);
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
        Assert.Equal("Hello", provider.GetLocalizedString(Key("Greeting"), ThisTestAssembly, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void GetLocalizedString_AssemblyWithNoResx_ThrowsLocalizationConfigurationException()
    {
        var provider = CreateProvider();

        var ex = Assert.Throws<LocalizationConfigurationException>(
            () => provider.GetLocalizedString(Key("Anything"), typeof(Meteion.Toolkit.WPF.Localization.Tests.Fixtures.NoResx.Marker).Assembly, CultureInfo.InvariantCulture));

        Assert.Contains("no embedded .resources files", ex.Message);
    }

    [Fact]
    public void GetLocalizedString_AssemblyWithMultipleUnrelatedResx_ThrowsLocalizationConfigurationException()
    {
        var provider = CreateProvider();
        var ambiguousAssembly = typeof(Meteion.Toolkit.WPF.Localization.Tests.Fixtures.MultipleResx.Marker).Assembly;

        var ex = Assert.Throws<LocalizationConfigurationException>(
            () => provider.GetLocalizedString(Key("Sample"), ambiguousAssembly, CultureInfo.InvariantCulture));

        Assert.Contains("multiple embedded .resources files", ex.Message);
    }

    [Theory]
    [InlineData("First")]
    [InlineData("Second")]
    public void GetLocalizedString_QualifiedKey_ResolvesFromTheNamedResx(string resxName)
    {
        var provider = CreateProvider();
        var assemblyName = MultipleResxAssembly.GetName().Name!;
        var key = LocalizationKey.Qualified(assemblyName, $"{assemblyName}.{resxName}", "Sample");

        var value = provider.GetLocalizedString(key, MultipleResxAssembly, CultureInfo.InvariantCulture);

        Assert.Equal(resxName, value);
    }

    [Fact]
    public void GetLocalizedString_QualifiedKeyNamingUnknownResx_ThrowsListingActualResxFiles()
    {
        var provider = CreateProvider();
        var assemblyName = MultipleResxAssembly.GetName().Name!;
        var key = LocalizationKey.Qualified(assemblyName, $"{assemblyName}.Third", "Sample");

        var ex = Assert.Throws<LocalizationConfigurationException>(
            () => provider.GetLocalizedString(key, MultipleResxAssembly, CultureInfo.InvariantCulture));

        Assert.Contains($"{assemblyName}.First", ex.Message);
        Assert.Contains($"{assemblyName}.Second", ex.Message);
    }

    [Fact]
    public void GetAvailableKeys_MultipleResx_ReturnsQualifiedKeysFromEveryResx()
    {
        var provider = CreateProvider();
        var assemblyName = MultipleResxAssembly.GetName().Name!;

        var keys = provider.GetAvailableKeys(MultipleResxAssembly).ToHashSet();

        Assert.Equal(new HashSet<string>
        {
            $"{assemblyName}/{assemblyName}.First:Sample",
            $"{assemblyName}/{assemblyName}.Second:Sample",
        }, keys);
    }
}
