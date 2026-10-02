using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Meteion.Toolkit.WPF.Converters;

/// <summary>
/// Converts a background color to an ideal foreground color (black or white) for text readability based on the specified background color.
/// </summary>
[ValueConversion(typeof(SolidColorBrush), typeof(SolidColorBrush))]
public sealed class BackgroundToForegroundConverter : IValueConverter, IMultiValueConverter
{
    /// <summary>
    /// Gets a static default instance of <see cref="BackgroundToForegroundConverter"/>.
    /// </summary>
    public static readonly BackgroundToForegroundConverter Instance = new();

    /// <summary>
    /// Determining Ideal Text Color Based on Specified Background Color
    /// http://www.codeproject.com/KB/GDI-plus/IdealTextColor.aspx
    /// </summary>
    /// <param name = "background">The background color.</param>
    /// <returns>The ideal foreground color.</returns>
    private static Color IdealTextColor(Color background)
    {
        // Weighted luma (0-255); backgrounds brighter than roughly 169 get black text, darker ones white.
        const int nThreshold = 86; // 105;
        var bgDelta = System.Convert.ToInt32(background.R * 0.299 + background.G * 0.587 + background.B * 0.114);
        var foreColor = 255 - bgDelta < nThreshold ? Colors.Black : Colors.White;
        return foreColor;
    }

    /// <summary>
    /// Converts a background brush to a black or white foreground brush with enough contrast.
    /// </summary>
    /// <param name="value">The background brush. Only <see cref="SolidColorBrush"/> values are analyzed.</param>
    /// <param name="targetType">The binding target type. Not used.</param>
    /// <param name="parameter">An optional converter parameter. Not used.</param>
    /// <param name="culture">The culture of the conversion. Not used.</param>
    /// <returns>A frozen black or white brush, or <see cref="Brushes.White"/> when the value is not a solid brush.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SolidColorBrush backgroundBrush)
        {
            var idealForegroundColor = IdealTextColor(backgroundBrush.Color);
            var foregroundBrush = new SolidColorBrush(idealForegroundColor);
            foregroundBrush.Freeze();
            return foregroundBrush;
        }

        return Brushes.White;
    }

    /// <summary>
    /// Not supported; conversion back is never meaningful for this converter.
    /// </summary>
    /// <param name="value">The value produced by the binding target. Not used.</param>
    /// <param name="targetType">The binding source type. Not used.</param>
    /// <param name="parameter">An optional converter parameter. Not used.</param>
    /// <param name="culture">The culture of the conversion. Not used.</param>
    /// <returns><see cref="DependencyProperty.UnsetValue"/>.</returns>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return DependencyProperty.UnsetValue;
    }

    /// <summary>
    /// Multi-binding variant: returns an explicit foreground brush when one is supplied, otherwise derives one from the background.
    /// </summary>
    /// <param name="values">Binding values: index 0 is the background brush, index 1 is an optional explicit foreground brush.</param>
    /// <param name="targetType">The binding target type. Passed through to the single-value conversion.</param>
    /// <param name="parameter">An optional converter parameter. Passed through to the single-value conversion.</param>
    /// <param name="culture">The culture of the conversion. Passed through to the single-value conversion.</param>
    /// <returns>The explicit foreground brush if present, otherwise the computed contrasting brush.</returns>
    public object? Convert(object[]? values, Type targetType, object? parameter, CultureInfo culture)
    {
        var titleBrush = values?.Length > 1 ? values[1] as Brush : null;
        if (titleBrush is not null)
        {
            return titleBrush;
        }

        var backgroundBrush = values?.Length > 0 ? values[0] as Brush : null;
        return Convert(backgroundBrush, targetType, parameter, culture);
    }

    /// <summary>
    /// Not supported; conversion back is never meaningful for this converter.
    /// </summary>
    /// <param name="value">The value produced by the binding target. Not used.</param>
    /// <param name="targetTypes">The binding source types, one per bound value.</param>
    /// <param name="parameter">An optional converter parameter. Not used.</param>
    /// <param name="culture">The culture of the conversion. Not used.</param>
    /// <returns>An array of <see cref="DependencyProperty.UnsetValue"/>, one per target type.</returns>
    public object[]? ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        return targetTypes.Select(t => DependencyProperty.UnsetValue).ToArray();
    }
}