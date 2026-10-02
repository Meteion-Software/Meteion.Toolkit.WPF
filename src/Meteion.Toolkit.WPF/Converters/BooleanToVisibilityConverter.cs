using System.Windows;
using System.Windows.Data;

namespace Meteion.Toolkit.WPF.Converters;

/// <summary>
/// Simply converts a boolean value to a Visibility value. True is converted to Visible, and False is converted to Collapsed. This converter does not support converting back from Visibility to boolean.
/// </summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class BooleanToVisibilityConverter : IValueConverter
{
    /// <summary>Gets a shared instance, for use with <c>x:Static</c>.</summary>
    public static readonly BooleanToVisibilityConverter Instance = new();

    /// <summary>
    /// Converts a boolean to a <see cref="Visibility"/>.
    /// </summary>
    /// <param name="value">The boolean to convert.</param>
    /// <param name="targetType">The binding target type. Not used.</param>
    /// <param name="parameter">An optional converter parameter. Not used.</param>
    /// <param name="culture">The culture of the conversion. Not used.</param>
    /// <returns>
    /// <see cref="Visibility.Visible"/> for true, <see cref="Visibility.Collapsed"/> for false, or
    /// <see cref="DependencyProperty.UnsetValue"/> when the value is not a boolean.
    /// </returns>
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? Visibility.Visible : Visibility.Collapsed;
        }

        return DependencyProperty.UnsetValue;
    }

    /// <summary>
    /// Not supported; conversion back is never meaningful for this converter.
    /// </summary>
    /// <param name="value">The value produced by the binding target. Not used.</param>
    /// <param name="targetType">The binding source type. Not used.</param>
    /// <param name="parameter">An optional converter parameter. Not used.</param>
    /// <param name="culture">The culture of the conversion. Not used.</param>
    /// <returns><see cref="DependencyProperty.UnsetValue"/>.</returns>
    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        return DependencyProperty.UnsetValue;
    }
}
