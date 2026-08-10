using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Meteion.Toolkit.WPF.Converters;

[ValueConversion(typeof(string), typeof(string))]
public sealed class ToUpperConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return str.ToUpper();
        }

        return DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return DependencyProperty.UnsetValue;
    }
}
