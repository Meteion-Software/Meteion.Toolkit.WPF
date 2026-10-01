using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Meteion.Toolkit.WPF.Converters;

/// <summary>
/// Visible when the value is a non-empty string, a non-empty collection, or any other non-null object; otherwise Collapsed.
/// </summary>
[ValueConversion(typeof(IEnumerable), typeof(Visibility))]
public class VisibleIfNotNullOrEmptyConverter : IValueConverter
{
    public static readonly VisibleIfNotNullOrEmptyConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return NullOrEmpty.Check(value) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return DependencyProperty.UnsetValue;
    }
}
