using System.Windows;
using System.Windows.Data;

namespace Meteion.Toolkit.WPF.Converters;


[ValueConversion(typeof(object), typeof(Visibility))]
public sealed class VisibleIfNullConverter : IValueConverter
{
    public static readonly VisibleIfNullConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        return value is null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        return DependencyProperty.UnsetValue;
    }
}
