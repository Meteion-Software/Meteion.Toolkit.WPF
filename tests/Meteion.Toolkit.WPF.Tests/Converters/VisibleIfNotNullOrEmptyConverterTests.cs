using Meteion.Toolkit.WPF.Converters;
using System.Globalization;
using System.Windows;

namespace Meteion.Toolkit.WPF.Tests.Converters;

public class VisibleIfNotNullOrEmptyConverterTests
{
    private static readonly VisibleIfNotNullOrEmptyConverter Converter = new();

    [Fact]
    public void Convert_Null_ReturnsCollapsed()
        => Assert.Equal(Visibility.Collapsed, Converter.Convert(null!, typeof(Visibility), null!, CultureInfo.InvariantCulture));

    [Fact]
    public void Convert_EmptyList_ReturnsCollapsed()
        => Assert.Equal(Visibility.Collapsed, Converter.Convert(new List<string>(), typeof(Visibility), null!, CultureInfo.InvariantCulture));

    [Fact]
    public void Convert_NonEmptyList_ReturnsVisible()
        => Assert.Equal(Visibility.Visible, Converter.Convert(new List<string> { "item" }, typeof(Visibility), null!, CultureInfo.InvariantCulture));

    [Fact]
    public void Convert_NonListNonNullValue_ReturnsVisible()
        => Assert.Equal(Visibility.Visible, Converter.Convert("not a list", typeof(Visibility), null!, CultureInfo.InvariantCulture));

    [Fact]
    public void ConvertBack_ReturnsUnsetValue()
        => Assert.Equal(DependencyProperty.UnsetValue, Converter.ConvertBack(Visibility.Visible, typeof(object), null!, CultureInfo.InvariantCulture));
}
