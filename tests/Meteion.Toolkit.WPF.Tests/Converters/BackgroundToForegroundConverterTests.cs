using Meteion.Toolkit.WPF.Converters;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace Meteion.Toolkit.WPF.Tests.Converters;

public class BackgroundToForegroundConverterTests
{
    private static readonly BackgroundToForegroundConverter Converter = new();

    [Fact]
    public void Convert_DarkBackground_ReturnsWhiteForeground()
    {
        var result = Converter.Convert(new SolidColorBrush(Colors.Black), typeof(SolidColorBrush), null, CultureInfo.InvariantCulture);

        Assert.Equal(Colors.White, Assert.IsType<SolidColorBrush>(result).Color);
    }

    [Fact]
    public void Convert_LightBackground_ReturnsBlackForeground()
    {
        var result = Converter.Convert(new SolidColorBrush(Colors.White), typeof(SolidColorBrush), null, CultureInfo.InvariantCulture);

        Assert.Equal(Colors.Black, Assert.IsType<SolidColorBrush>(result).Color);
    }

    [Fact]
    public void Convert_ResultIsFrozen()
    {
        var result = Converter.Convert(new SolidColorBrush(Colors.Black), typeof(SolidColorBrush), null, CultureInfo.InvariantCulture);

        Assert.True(Assert.IsType<SolidColorBrush>(result).IsFrozen);
    }

    [Fact]
    public void Convert_NonBrushValue_ReturnsWhiteBrushFallback()
    {
        var result = Converter.Convert("not a brush", typeof(SolidColorBrush), null, CultureInfo.InvariantCulture);

        Assert.Same(Brushes.White, result);
    }

    [Fact]
    public void ConvertBack_ReturnsUnsetValue()
    {
        var result = Converter.ConvertBack(Brushes.White, typeof(SolidColorBrush), null, CultureInfo.InvariantCulture);

        Assert.Equal(DependencyProperty.UnsetValue, result);
    }

    [Fact]
    public void MultiConvert_SecondValueIsBrush_ReturnsItDirectly()
    {
        var titleBrush = Brushes.Red;
        var values = new object[] { new SolidColorBrush(Colors.Black), titleBrush };

        var result = Converter.Convert(values, typeof(Brush), null, CultureInfo.InvariantCulture);

        Assert.Same(titleBrush, result);
    }

    [Fact]
    public void MultiConvert_NoSecondBrush_DelegatesToBackgroundConversion()
    {
        var values = new object[] { new SolidColorBrush(Colors.Black) };

        var result = Converter.Convert(values, typeof(Brush), null, CultureInfo.InvariantCulture);

        Assert.Equal(Colors.White, Assert.IsType<SolidColorBrush>(result).Color);
    }

    [Fact]
    public void MultiConvertBack_ReturnsUnsetValueForEachTargetType()
    {
        var targetTypes = new[] { typeof(Brush), typeof(Brush) };

        var result = Converter.ConvertBack(Brushes.White, targetTypes, null, CultureInfo.InvariantCulture);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Length);
        Assert.All(result, v => Assert.Equal(DependencyProperty.UnsetValue, v));
    }
}
