using Meteion.Toolkit.WPF.Localization.Converters;
using Meteion.Toolkit.WPF.Localization.Tests.Fakes;
using System.Globalization;
using System.Windows;

namespace Meteion.Toolkit.WPF.Localization.Tests.Converters;

public class CultureAwareFormatConverterTests
{
    [Fact]
    public void Convert_UsesLocalizationServiceCurrentCulture_NotTheCultureArgument()
    {
        var service = new FakeLocalizationService { CurrentCulture = new CultureInfo("fr-FR") };
        var converter = new CultureAwareFormatConverter(service, "N2");

        // The culture WPF's binding pipeline hands the converter (en-US here) must be
        // ignored in favor of ILocalizationService.CurrentCulture — that's the whole point:
        // WPF's own culture argument is derived from FrameworkElement.Language, which
        // defaults to en-US regardless of the app's selected language.
        var result = converter.Convert(1234.5, typeof(string), null!, new CultureInfo("en-US"));

        Assert.Equal(1234.5.ToString("N2", new CultureInfo("fr-FR")), result);
    }

    [Fact]
    public void Convert_MultiValueOverload_UsesFirstValue()
    {
        var service = new FakeLocalizationService { CurrentCulture = new CultureInfo("de-DE") };
        var converter = new CultureAwareFormatConverter(service, "C");

        var result = converter.Convert([1234.5, "ignored"], typeof(string), null!, CultureInfo.InvariantCulture);

        Assert.Equal(1234.5.ToString("C", new CultureInfo("de-DE")), result);
    }

    [Fact]
    public void Convert_ConverterParameterOverridesFormatString()
    {
        var service = new FakeLocalizationService { CurrentCulture = new CultureInfo("en-US") };
        var converter = new CultureAwareFormatConverter(service, "N2");

        var result = converter.Convert(1234.5, typeof(string), "C", CultureInfo.InvariantCulture);

        Assert.Equal(1234.5.ToString("C", new CultureInfo("en-US")), result);
    }

    [Fact]
    public void Convert_ValueIsNotIFormattable_ReturnsUnsetValue()
    {
        var service = new FakeLocalizationService { CurrentCulture = CultureInfo.InvariantCulture };
        var converter = new CultureAwareFormatConverter(service);

        var result = converter.Convert(new object(), typeof(string), null!, CultureInfo.InvariantCulture);

        Assert.Equal(DependencyProperty.UnsetValue, result);
    }

    [Fact]
    public void ConvertBack_Throws()
    {
        var converter = new CultureAwareFormatConverter(new FakeLocalizationService());

        Assert.Throws<NotSupportedException>(() =>
            converter.ConvertBack("1234", typeof(double), null!, CultureInfo.InvariantCulture));
        Assert.Throws<NotSupportedException>(() =>
            converter.ConvertBack("1234", [typeof(double)], null!, CultureInfo.InvariantCulture));
    }
}
