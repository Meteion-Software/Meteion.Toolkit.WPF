using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Meteion.Toolkit.WPF.Converters;

/// <summary>
/// Visible when the value is null, an empty string, or an empty collection; otherwise Collapsed.
/// </summary>
[ValueConversion(typeof(IEnumerable), typeof(Visibility))]
public class VisibleIfNullOrEmptyConverter : IValueConverter
{
    public static readonly VisibleIfNullOrEmptyConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return NullOrEmpty.Check(value) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return DependencyProperty.UnsetValue;
    }
}
