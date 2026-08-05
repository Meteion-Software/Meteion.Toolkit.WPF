
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Meteion.Toolkit.WPF.Converters;

[ValueConversion(typeof(IList), typeof(Visibility))]
public class VisibleIfNullOrEmptyConverter : IValueConverter
{
    public static readonly VisibleIfNullOrEmptyConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null)
        {
            return Visibility.Visible;
        }

        if (value is IList collection)
        {
            if (collection.Count == 0)
            {
                return Visibility.Visible;
            }
            else
            {
                return Visibility.Collapsed;
            }
        }
        else
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return DependencyProperty.UnsetValue;
    }
}
