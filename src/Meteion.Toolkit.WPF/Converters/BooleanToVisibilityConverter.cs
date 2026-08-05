using System.Windows;
using System.Windows.Data;

namespace Meteion.Toolkit.WPF.Converters;

/// <summary>
/// Simply converts a boolean value to a Visibility value. True is converted to Visible, and False is converted to Collapsed. This converter does not support converting back from Visibility to boolean.
/// </summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class BooleanToVisibilityConverter : IValueConverter
{
    public static readonly BooleanToVisibilityConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? Visibility.Visible : Visibility.Collapsed;
        }

        return DependencyProperty.UnsetValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        return DependencyProperty.UnsetValue;
    }
}
