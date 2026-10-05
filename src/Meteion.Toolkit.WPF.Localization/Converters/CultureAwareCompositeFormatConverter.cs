using Meteion.Toolkit.Localization.Abstractions;
using Meteion.Toolkit.WPF.Localization.Extensions;
using System.Globalization;
using System.Windows.Data;

namespace Meteion.Toolkit.WPF.Localization.Converters;

/// <summary>
/// <see cref="MultiBinding.Converter"/> that applies several bound values to one composite format
/// string (e.g. <c>"{0:C} of {1:C}"</c>) using <see cref="ILocalizationService.CurrentCulture"/>.
/// The values arrive as <c>[culture trigger, values...]</c>; the trigger exists only so the result
/// re-runs when the culture changes.
/// </summary>
/// <param name="service">The service supplying the current culture.</param>
/// <param name="formatString">The composite format string.</param>
/// <param name="behavior">How a format failure is handled - the app's <see cref="LocalizationOptions.MissingKeyBehavior"/>.</param>
internal sealed class CultureAwareCompositeFormatConverter(
    ILocalizationService service,
    string formatString,
    MissingResourceBehavior behavior) : IMultiValueConverter
{
    /// <summary>
    /// Formats every value after the first (the culture trigger).
    /// </summary>
    /// <param name="values">The bound values, in the layout described on the type.</param>
    /// <param name="targetType">Ignored.</param>
    /// <param name="parameter">Ignored.</param>
    /// <param name="culture">Ignored; the localization service's culture is used instead.</param>
    /// <returns>The formatted text.</returns>
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture) =>
        FormatArguments.FormatForBinding(
            service.CurrentCulture, formatString, values.Length > 1 ? values[1..] : [], behavior);

    /// <summary>Not supported; this converter is one-way.</summary>
    /// <param name="value">Unused.</param>
    /// <param name="targetTypes">Unused.</param>
    /// <param name="parameter">Unused.</param>
    /// <param name="culture">Unused.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException($"{nameof(CultureAwareCompositeFormatConverter)} only supports one-way binding.");
}
