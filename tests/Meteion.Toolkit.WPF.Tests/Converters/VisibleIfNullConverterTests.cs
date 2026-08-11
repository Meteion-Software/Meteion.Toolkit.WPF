using Meteion.Toolkit.WPF.Converters;
using System.Globalization;
using System.Windows;

namespace Meteion.Toolkit.WPF.Tests.Converters;

public class VisibleIfNullConverterTests
{
    private static readonly VisibleIfNullConverter Converter = new();

    [Fact]
    public void Convert_Null_ReturnsVisible()
        => Assert.Equal(Visibility.Visible, Converter.Convert(null, typeof(Visibility), null, CultureInfo.InvariantCulture));

    [Fact]
    public void Convert_NonNull_ReturnsCollapsed()
        => Assert.Equal(Visibility.Collapsed, Converter.Convert("something", typeof(Visibility), null, CultureInfo.InvariantCulture));

    [Fact]
    public void ConvertBack_ReturnsUnsetValue()
        => Assert.Equal(DependencyProperty.UnsetValue, Converter.ConvertBack(Visibility.Visible, typeof(object), null, CultureInfo.InvariantCulture));
}
