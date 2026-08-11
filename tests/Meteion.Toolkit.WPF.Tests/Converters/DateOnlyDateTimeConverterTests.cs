using Meteion.Toolkit.WPF.Converters;
using System.Globalization;
using System.Windows;

namespace Meteion.Toolkit.WPF.Tests.Converters;

public class DateOnlyDateTimeConverterTests
{
    private static readonly DateOnlyDateTimeConverter Converter = new();

    [Fact]
    public void Convert_DateOnly_ReturnsDateTimeAtMidnight()
    {
        var dateOnly = new DateOnly(2024, 6, 15);

        var result = Converter.Convert(dateOnly, typeof(DateTime), null!, CultureInfo.InvariantCulture);

        Assert.Equal(new DateTime(2024, 6, 15, 0, 0, 0), result);
    }

    [Fact]
    public void Convert_DateTime_ReturnsDateOnlyDiscardingTime()
    {
        var dateTime = new DateTime(2024, 6, 15, 13, 45, 0);

        var result = Converter.Convert(dateTime, typeof(DateOnly), null!, CultureInfo.InvariantCulture);

        Assert.Equal(new DateOnly(2024, 6, 15), result);
    }

    [Fact]
    public void Convert_UnsupportedType_ReturnsUnsetValue()
    {
        var result = Converter.Convert("not a date", typeof(DateTime), null!, CultureInfo.InvariantCulture);

        Assert.Equal(DependencyProperty.UnsetValue, result);
    }

    [Fact]
    public void ConvertBack_DateOnly_BehavesSameAsConvert()
    {
        var dateOnly = new DateOnly(2024, 6, 15);

        var result = Converter.ConvertBack(dateOnly, typeof(DateTime), null!, CultureInfo.InvariantCulture);

        Assert.Equal(new DateTime(2024, 6, 15, 0, 0, 0), result);
    }

    [Fact]
    public void ConvertBack_DateTime_BehavesSameAsConvert()
    {
        var dateTime = new DateTime(2024, 6, 15, 13, 45, 0);

        var result = Converter.ConvertBack(dateTime, typeof(DateOnly), null!, CultureInfo.InvariantCulture);

        Assert.Equal(new DateOnly(2024, 6, 15), result);
    }
}
