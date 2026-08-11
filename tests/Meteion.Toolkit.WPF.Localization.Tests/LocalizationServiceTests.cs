using Meteion.Toolkit.Localization.Abstractions;
using Meteion.Toolkit.WPF.Localization.Tests.Fakes;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace Meteion.Toolkit.WPF.Localization.Tests;

public class LocalizationServiceTests
{
    private static readonly Assembly SomeAssembly = typeof(LocalizationServiceTests).Assembly;

    private static LocalizationService CreateService(
        FakeLocalizationProvider provider, LocalizationOptions? options = null)
        => new(provider, Options.Create(options ?? new LocalizationOptions()));

    [Fact]
    public void Constructor_NoDefaultCultureConfigured_UsesCurrentUICulture()
    {
        var service = CreateService(new FakeLocalizationProvider());

        Assert.Equal(CultureInfo.CurrentUICulture, service.CurrentCulture);
    }

    [Fact]
    public void Constructor_DefaultCultureConfigured_UsesIt()
    {
        var configuredCulture = new CultureInfo("fr-CA");
        var service = CreateService(new FakeLocalizationProvider(),
            new LocalizationOptions { DefaultCulture = configuredCulture });

        Assert.Equal(configuredCulture, service.CurrentCulture);
    }

    [Fact]
    public void GetString_ExplicitAssembly_PassesItStraightToProvider()
    {
        var provider = new FakeLocalizationProvider { ValueToReturn = "Hello" };
        var service = CreateService(provider);

        var result = service.GetString("Greeting", SomeAssembly);

        Assert.Equal("Hello", result);
        Assert.Same(SomeAssembly, provider.LastAssembly);
        Assert.Equal("Greeting", provider.LastKey);
    }

    [Fact]
    public void GetString_NoAssembly_FallsBackToDefaultAssembly()
    {
        var provider = new FakeLocalizationProvider { ValueToReturn = "Hello" };
        var service = CreateService(provider, new LocalizationOptions { DefaultAssembly = SomeAssembly });

        service.GetString("Greeting");

        Assert.Same(SomeAssembly, provider.LastAssembly);
    }

    [Fact]
    public void GetString_NoAssemblyAndNoDefaultConfigured_ThrowsLocalizationConfigurationException()
    {
        var service = CreateService(new FakeLocalizationProvider());

        Assert.Throws<LocalizationConfigurationException>(() => service.GetString("Greeting"));
    }

    [Fact]
    public void GetString_PassesCurrentCultureToProvider()
    {
        var provider = new FakeLocalizationProvider { ValueToReturn = "Hello" };
        var service = CreateService(provider, new LocalizationOptions { DefaultAssembly = SomeAssembly });
        var culture = new CultureInfo("ja-JP");
        service.CurrentCulture = culture;

        service.GetString("Greeting");

        Assert.Equal(culture, provider.LastCulture);
    }

    [Theory]
    [InlineData(MissingResourceBehavior.ReturnKey, "Greeting")]
    [InlineData(MissingResourceBehavior.ReturnEmptyString, "")]
    public void GetString_KeyNotFound_ReturnsAccordingToMissingKeyBehavior(
        MissingResourceBehavior behavior, string expected)
    {
        var provider = new FakeLocalizationProvider { ValueToReturn = null };
        var service = CreateService(provider,
            new LocalizationOptions { DefaultAssembly = SomeAssembly, MissingKeyBehavior = behavior });

        var result = service.GetString("Greeting");

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetString_KeyNotFoundAndThrowConfigured_ThrowsLocalizationKeyNotFoundExceptionWithDetails()
    {
        var provider = new FakeLocalizationProvider { ValueToReturn = null };
        var service = CreateService(provider, new LocalizationOptions
        {
            DefaultAssembly = SomeAssembly,
            MissingKeyBehavior = MissingResourceBehavior.ThrowException
        });

        var ex = Assert.Throws<LocalizationKeyNotFoundException>(() => service.GetString("Greeting"));

        Assert.Equal("Greeting", ex.Key);
        Assert.Same(SomeAssembly, ex.ResourceAssembly);
    }

    [Fact]
    public void CurrentCulture_SetToDifferentValue_RaisesCultureChangedAndPropertyChanged()
    {
        var service = CreateService(new FakeLocalizationProvider());
        var newCulture = new CultureInfo("ja-JP");

        CultureChangedEventArgs? cultureChangedArgs = null;
        PropertyChangedEventArgs? propertyChangedArgs = null;
        service.CultureChanged += (_, e) => cultureChangedArgs = e;
        service.PropertyChanged += (_, e) => propertyChangedArgs = e;

        service.CurrentCulture = newCulture;

        Assert.Equal(newCulture, cultureChangedArgs?.Culture);
        Assert.Equal(nameof(ILocalizationService.CurrentCulture), propertyChangedArgs?.PropertyName);
        Assert.Equal(newCulture, service.CurrentCulture);
    }

    [Fact]
    public void CurrentCulture_SetToSameValue_DoesNotRaiseEvents()
    {
        var options = new LocalizationOptions { DefaultCulture = new CultureInfo("en-CA") };
        var service = CreateService(new FakeLocalizationProvider(), options);

        var raised = false;
        service.CultureChanged += (_, _) => raised = true;
        service.PropertyChanged += (_, _) => raised = true;

        service.CurrentCulture = new CultureInfo("en-CA");

        Assert.False(raised);
    }
}
