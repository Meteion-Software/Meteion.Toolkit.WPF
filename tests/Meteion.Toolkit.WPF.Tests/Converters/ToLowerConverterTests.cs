using Meteion.Toolkit.WPF.Converters;
using System.Globalization;
using System.Windows;

namespace Meteion.Toolkit.WPF.Tests.Converters;

public class ToLowerConverterTests
{
    private static readonly ToLowerConverter Converter = new();

    [Fact]
    public void Convert_String_ReturnsLowercase()
        => Assert.Equal("hello world", Converter.Convert("Hello World", typeof(string), null!, CultureInfo.InvariantCulture));

    [Fact]
    public void Convert_NonString_ReturnsUnsetValue()
        => Assert.Equal(DependencyProperty.UnsetValue, Converter.Convert(42, typeof(string), null!, CultureInfo.InvariantCulture));

    [Fact]
    public void ConvertBack_AlwaysReturnsUnsetValue()
        => Assert.Equal(DependencyProperty.UnsetValue, Converter.ConvertBack("hello", typeof(string), null!, CultureInfo.InvariantCulture));
}
