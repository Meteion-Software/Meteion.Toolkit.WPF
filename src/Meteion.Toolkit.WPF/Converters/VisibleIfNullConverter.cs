using System.Windows;
using System.Windows.Data;

namespace Meteion.Toolkit.WPF.Converters;

/// <summary>
/// Visible when the value is null; otherwise Collapsed.
/// </summary>
[ValueConversion(typeof(object), typeof(Visibility))]
public sealed class VisibleIfNullConverter : IValueConverter
{
    /// <summary>Gets a shared instance, for use with <c>x:Static</c>.</summary>
    public static readonly VisibleIfNullConverter Instance = new();

    /// <summary>
    /// Converts a null check to a <see cref="Visibility"/>.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <param name="targetType">The binding target type. Not used.</param>
    /// <param name="parameter">An optional converter parameter. Not used.</param>
    /// <param name="culture">The culture of the conversion. Not used.</param>
    /// <returns><see cref="Visibility.Visible"/> when the value is null, otherwise <see cref="Visibility.Collapsed"/>.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        return value is null ? Visibility.Visible : Visibility.Collapsed;
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
