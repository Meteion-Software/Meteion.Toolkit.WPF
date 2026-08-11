using Meteion.Toolkit.WPF.Converters;
using System.Globalization;
using System.Windows;

namespace Meteion.Toolkit.WPF.Tests.Converters;

public class BooleanToVisibilityConverterTests
{
    private static readonly BooleanToVisibilityConverter Converter = new();

    [Fact]
    public void Convert_True_ReturnsVisible()
        => Assert.Equal(Visibility.Visible, Converter.Convert(true, typeof(Visibility), null, CultureInfo.InvariantCulture));

    [Fact]
    public void Convert_False_ReturnsCollapsed()
        => Assert.Equal(Visibility.Collapsed, Converter.Convert(false, typeof(Visibility), null, CultureInfo.InvariantCulture));

    [Fact]
    public void Convert_NonBoolean_ReturnsUnsetValue()
        => Assert.Equal(DependencyProperty.UnsetValue, Converter.Convert("not a bool", typeof(Visibility), null, CultureInfo.InvariantCulture));

    [Fact]
    public void ConvertBack_ReturnsUnsetValue()
        => Assert.Equal(DependencyProperty.UnsetValue, Converter.ConvertBack(Visibility.Visible, typeof(bool), null, CultureInfo.InvariantCulture));
}
