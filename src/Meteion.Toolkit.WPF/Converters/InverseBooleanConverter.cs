using System.Windows;
using System.Windows.Data;

namespace Meteion.Toolkit.WPF.Converters;

/// <summary>
/// Inverses a boolean.
/// </summary>
[ValueConversion(typeof(bool), typeof(bool))]
public sealed class InverseBooleanConverter : IValueConverter
{
    public static readonly InverseBooleanToVisibilityConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool b)
        {
            return !b;
        }

        return DependencyProperty.UnsetValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        return DependencyProperty.UnsetValue;
    }
}
