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

    private static string QualifiedKey(string key) => $"{SomeAssembly.GetName().Name}/Some.Strings:{key}";

    [Fact]
    public void GetString_QualifiedKey_ResolvesNamedAssemblyAndBaseName()
    {
        var provider = new FakeLocalizationProvider { ValueToReturn = "Hello" };
        var service = CreateService(provider);

        var result = service.GetString(QualifiedKey("Greeting"));

        Assert.Equal("Hello", result);
        Assert.Same(SomeAssembly, provider.LastAssembly);
        Assert.Equal("Some.Strings", provider.LastLocalizationKey?.BaseName);
        Assert.Equal("Greeting", provider.LastLocalizationKey?.Key);
    }

    [Fact]
    public void GetString_QualifiedKeyWithMatchingAssembly_Resolves()
    {
        var provider = new FakeLocalizationProvider { ValueToReturn = "Hello" };
        var service = CreateService(provider);

        Assert.Equal("Hello", service.GetString(QualifiedKey("Greeting"), SomeAssembly));
    }

    [Fact]
    public void GetString_QualifiedKeyWithDifferentAssembly_ThrowsLocalizationConfigurationException()
    {
        var service = CreateService(new FakeLocalizationProvider());

        Assert.Throws<LocalizationConfigurationException>(
            () => service.GetString(QualifiedKey("Greeting"), typeof(object).Assembly));
    }

    [Fact]
    public void GetString_Source_QualifiesUnqualifiedKey()
    {
        var provider = new FakeLocalizationProvider { ValueToReturn = "Hello" };
        var service = CreateService(provider);

        service.GetString("Greeting", $"{SomeAssembly.GetName().Name}/Some.Strings");

        Assert.Equal(QualifiedKey("Greeting"), provider.LastKey);
        Assert.Same(SomeAssembly, provider.LastAssembly);
    }

    [Theory]
    [InlineData("NotASource")]
    [InlineData("Asm/Base:Key")]
    public void GetString_InvalidSource_ThrowsLocalizationConfigurationException(string source)
    {
        var service = CreateService(new FakeLocalizationProvider());

        Assert.Throws<LocalizationConfigurationException>(() => service.GetString("Greeting", source));
    }

    [Fact]
    public void GetString_SourceWithQualifiedKey_ThrowsLocalizationConfigurationException()
    {
        var service = CreateService(new FakeLocalizationProvider());

        Assert.Throws<LocalizationConfigurationException>(
            () => service.GetString(QualifiedKey("Greeting"), $"{SomeAssembly.GetName().Name}/Some.Strings"));
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
    public void GetFormattedString_AppliesArgsToLocalizedTemplate()
    {
        var provider = new FakeLocalizationProvider { ValueToReturn = "Hello {0}, you have {1} items" };
        var service = CreateService(provider, new LocalizationOptions { DefaultAssembly = SomeAssembly });

        Assert.Equal("Hello Ada, you have 3 items", service.GetFormattedString("Greeting", "Ada", 3));
    }

    [Fact]
    public void GetFormattedString_StringArgIsAnArgument_NotASource()
    {
        var provider = new FakeLocalizationProvider { ValueToReturn = "Hi {0}" };
        var service = CreateService(provider, new LocalizationOptions { DefaultAssembly = SomeAssembly });

        // The reason this isn't a GetString overload: a lone string must be formatted, not read as a source.
        Assert.Equal("Hi Ada", service.GetFormattedString("Greeting", "Ada"));
        Assert.Same(SomeAssembly, provider.LastAssembly);
    }

    [Fact]
    public void GetFormattedString_ExplicitAssembly_ResolvesAgainstItThenFormats()
    {
        var provider = new FakeLocalizationProvider { ValueToReturn = "{0}!" };
        var service = CreateService(provider);

        var result = service.GetFormattedString("Greeting", SomeAssembly, ["Ada"]);

        Assert.Equal("Ada!", result);
        Assert.Same(SomeAssembly, provider.LastAssembly);
    }

    [Fact]
    public void GetFormattedString_Source_ResolvesFromSourceThenFormats()
    {
        var provider = new FakeLocalizationProvider { ValueToReturn = "{0}!" };
        var service = CreateService(provider);

        var result = service.GetFormattedString("Greeting", $"{SomeAssembly.GetName().Name}/Some.Strings", ["Ada"]);

        Assert.Equal("Ada!", result);
        Assert.Equal("Greeting", provider.LastLocalizationKey?.Key);
    }

    [Fact]
    public void GetFormattedString_UsesCurrentCultureForPlaceholders()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            var provider = new FakeLocalizationProvider { ValueToReturn = "{0:N0}" };
            var service = CreateService(provider, new LocalizationOptions { DefaultAssembly = SomeAssembly });
            service.CurrentCulture = new CultureInfo("de-DE");

            Assert.Equal("1.234.567", service.GetFormattedString("Total", 1234567));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Theory]
    [InlineData(MissingResourceBehavior.ReturnKey, "Hello {0} {1}")]
    [InlineData(MissingResourceBehavior.ReturnEmptyString, "")]
    public void GetFormattedString_TooFewArgs_ReturnsAccordingToMissingKeyBehavior(
        MissingResourceBehavior behavior, string expected)
    {
        var provider = new FakeLocalizationProvider { ValueToReturn = "Hello {0} {1}" };
        var service = CreateService(provider,
            new LocalizationOptions { DefaultAssembly = SomeAssembly, MissingKeyBehavior = behavior });

        Assert.Equal(expected, service.GetFormattedString("Greeting", "Ada"));
    }

    [Fact]
    public void GetFormattedString_TooFewArgsAndThrowConfigured_ThrowsConfigurationException()
    {
        var provider = new FakeLocalizationProvider { ValueToReturn = "Hello {0} {1}" };
        var service = CreateService(provider, new LocalizationOptions
        {
            DefaultAssembly = SomeAssembly,
            MissingKeyBehavior = MissingResourceBehavior.ThrowException,
        });

        Assert.Throws<LocalizationConfigurationException>(() => service.GetFormattedString("Greeting", "Ada"));
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

    [Fact]
    public void CurrentCulture_SetToDifferentValue_SyncsThreadCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            var service = CreateService(new FakeLocalizationProvider());
            var newCulture = new CultureInfo("ja-JP");

            service.CurrentCulture = newCulture;

            // Code that formats without an explicit culture/provider (DateTime.ToString(),
            // decimal.ToString(), resx lookups via CurrentUICulture, ...) must follow the
            // app's selected language rather than silently keeping the OS locale.
            Assert.Equal(newCulture, CultureInfo.CurrentCulture);
            Assert.Equal(newCulture, CultureInfo.CurrentUICulture);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public void Constructor_DefaultCultureConfigured_SyncsThreadCultureImmediately()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            var configuredCulture = new CultureInfo("fr-CA");

            CreateService(new FakeLocalizationProvider(), new LocalizationOptions { DefaultCulture = configuredCulture });

            // The starting culture must be applied too, not just later changes — otherwise a
            // DefaultCulture differing from the OS locale would only take effect once someone
            // explicitly re-sets CurrentCulture after construction.
            Assert.Equal(configuredCulture, CultureInfo.CurrentCulture);
            Assert.Equal(configuredCulture, CultureInfo.CurrentUICulture);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public void CurrentCulture_SetToSameValue_DoesNotResyncThreadCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            var options = new LocalizationOptions { DefaultCulture = new CultureInfo("en-CA") };
            var service = CreateService(new FakeLocalizationProvider(), options);

            // Deliberately mutate the ambient culture after construction to prove a no-op
            // set (same culture as already current) doesn't stomp back over it.
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            service.CurrentCulture = new CultureInfo("en-CA");

            Assert.Equal(new CultureInfo("de-DE"), CultureInfo.CurrentCulture);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }
}
