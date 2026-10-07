#if NET6_0_OR_GREATER
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Meteion.Toolkit.WPF.Converters;

/// <summary>
/// Converters a DateOnly to a DateTime and vice versa. When converting from DateOnly to DateTime, the time component is set to TimeOnly.MinValue (00:00:00). When converting from DateTime to DateOnly, the date component is extracted and the time component is discarded.
/// </summary>
[ValueConversion(typeof(DateOnly), typeof(DateTime))]
[ValueConversion(typeof(DateTime), typeof(DateOnly))]
public class DateOnlyToDateTimeConverter : IValueConverter
{
    /// <summary>Gets a shared instance, for use with <c>x:Static</c>.</summary>
    public static DateOnlyToDateTimeConverter Instance { get; } = new DateOnlyToDateTimeConverter();

    /// <summary>
    /// Converts a <see cref="DateOnly"/> to a <see cref="DateTime"/> at midnight, or a <see cref="DateTime"/> to a
    /// <see cref="DateOnly"/>.
    /// </summary>
    /// <param name="value">The <see cref="DateOnly"/> or <see cref="DateTime"/> to convert.</param>
    /// <param name="targetType">The binding target type. Not used.</param>
    /// <param name="parameter">An optional converter parameter. Not used.</param>
    /// <param name="culture">The culture of the conversion. Not used.</param>
    /// <returns>The converted date, or <see cref="DependencyProperty.UnsetValue"/> for any other input type.</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateOnly dateOnly)
        {
            return dateOnly.ToDateTime(TimeOnly.MinValue);
        }
        else if (value is DateTime dateTime)
        {
            return DateOnly.FromDateTime(dateTime);
        }
        else
        {
            return DependencyProperty.UnsetValue;
        }
    }

    /// <summary>
    /// Performs the reverse conversion; the conversion is symmetric, so this is the same as
    /// <see cref="Convert(object, Type, object, CultureInfo)"/>.
    /// </summary>
    /// <param name="value">The <see cref="DateOnly"/> or <see cref="DateTime"/> to convert.</param>
    /// <param name="targetType">The binding source type. Not used.</param>
    /// <param name="parameter">An optional converter parameter. Not used.</param>
    /// <param name="culture">The culture of the conversion. Not used.</param>
    /// <returns>The converted date, or <see cref="DependencyProperty.UnsetValue"/> for any other input type.</returns>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Convert(value, targetType, parameter, culture);
    }
}
#endif
