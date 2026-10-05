using System.Globalization;
using System.Windows.Data;

namespace Meteion.Toolkit.WPF.Localization.Extensions;

/// <summary>
/// <see cref="MultiBinding.Converter"/> that resolves a resource key into localized text and
/// applies format arguments to it. The values arrive as
/// <c>[key (only when the key is bound), culture trigger, args...]</c>; the trigger exists only
/// so the result re-runs when the culture changes.
/// </summary>
/// <param name="request">The lookup settings used to resolve the key.</param>
/// <param name="fixedKey">The literal key, or <see langword="null"/> when the key comes from a binding.</param>
internal sealed class FormattedLocalizationConverter(LocalizationRequest request, string? fixedKey) : IMultiValueConverter
{
    /// <summary>
    /// Resolves the key and formats it with the remaining values.
    /// </summary>
    /// <param name="values">The bound values, in the layout described on the type.</param>
    /// <param name="targetType">Ignored.</param>
    /// <param name="parameter">Ignored.</param>
    /// <param name="culture">Ignored; the localization service's culture is used instead.</param>
    /// <returns>The formatted text, or an empty string when there is no key.</returns>
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        var template = fixedKey is null
            ? request.ResolveBoundKey(values.Length > 0 ? values[0] : null)
            : request.ResolveForBinding(fixedKey);

        var argsStart = fixedKey is null ? 2 : 1;
        var args = values.Length > argsStart ? values[argsStart..] : [];

        return request.FormatForBinding(template, args);
    }

    /// <summary>Not supported; localized text cannot be converted back to a key.</summary>
    /// <param name="value">Unused.</param>
    /// <param name="targetTypes">Unused.</param>
    /// <param name="parameter">Unused.</param>
    /// <param name="culture">Unused.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException($"{nameof(FormattedLocalizationConverter)} only supports one-way binding.");
}
