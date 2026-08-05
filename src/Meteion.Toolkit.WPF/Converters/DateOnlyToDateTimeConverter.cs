using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Meteion.Toolkit.WPF.Converters;

/// <summary>
/// Converters a DateOnly to a DateTime and vice versa. When converting from DateOnly to DateTime, the time component is set to TimeOnly.MinValue (00:00:00). When converting from DateTime to DateOnly, the date component is extracted and the time component is discarded.
/// </summary>
[ValueConversion(typeof(DateOnly), typeof(DateTime))]
[ValueConversion(typeof(DateTime), typeof(DateOnly))]
public class DateOnlyDateTimeConverter : IValueConverter
{
    public static DateOnlyDateTimeConverter Instance { get; } = new DateOnlyDateTimeConverter();

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

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Convert(value, targetType, parameter, culture);
    }
}