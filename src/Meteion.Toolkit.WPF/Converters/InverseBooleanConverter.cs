using System.Windows;
using System.Windows.Data;

namespace Meteion.Toolkit.WPF.Converters;

/// <summary>
/// Inverses a boolean.
/// </summary>
[ValueConversion(typeof(bool), typeof(bool))]
public sealed class InverseBooleanConverter : IValueConverter
{
    /// <summary>Gets a shared instance, for use with <c>x:Static</c>.</summary>
    public static readonly InverseBooleanConverter Instance = new();

    /// <summary>
    /// Negates a boolean.
    /// </summary>
    /// <param name="value">The boolean to negate.</param>
    /// <param name="targetType">The binding target type. Not used.</param>
    /// <param name="parameter">An optional converter parameter. Not used.</param>
    /// <param name="culture">The culture of the conversion. Not used.</param>
    /// <returns>The negated boolean, or <see cref="DependencyProperty.UnsetValue"/> when the value is not a boolean.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool b)
        {
            return !b;
        }

        return DependencyProperty.UnsetValue;
    }

    /// <summary>
    /// Not supported; conversion back is not implemented for this converter.
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
