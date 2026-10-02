using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace Meteion.Toolkit.WPF.Converters;

/// <summary>
/// Converts a string to lower case using the current culture.
/// </summary>
[ValueConversion(typeof(string), typeof(string))]
public sealed class ToLowerConverter : IValueConverter
{
    /// <summary>Gets a shared instance, for use with <c>x:Static</c>.</summary>
    public static readonly ToLowerConverter Instance = new();

    /// <summary>
    /// Converts a string to lower case.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <param name="targetType">The binding target type. Not used.</param>
    /// <param name="parameter">An optional converter parameter. Not used.</param>
    /// <param name="culture">The culture of the conversion. Not used; the current culture is applied.</param>
    /// <returns>The lower-case string, or <see cref="DependencyProperty.UnsetValue"/> when the value is not a string.</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return str.ToLower();
        }

        return DependencyProperty.UnsetValue;
    }

    /// <summary>
    /// Not supported; the original casing cannot be restored.
    /// </summary>
    /// <param name="value">The value produced by the binding target. Not used.</param>
    /// <param name="targetType">The binding source type. Not used.</param>
    /// <param name="parameter">An optional converter parameter. Not used.</param>
    /// <param name="culture">The culture of the conversion. Not used.</param>
    /// <returns><see cref="DependencyProperty.UnsetValue"/>.</returns>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return DependencyProperty.UnsetValue;
    }
}
