using Meteion.Toolkit.Localization.Abstractions;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;

namespace Meteion.Toolkit.WPF.Localization.Extensions;

/// <summary>
/// <see cref="MultiBinding.Converter"/> that resolves a <see cref="LocalizedValueExtension.KeyBinding"/>
/// value into localized text. Combined with a <see cref="CultureChangeTrigger"/> as the second
/// input, so the result re-resolves whenever either the bound key or the active culture changes.
/// Used for every <c>DependencyProperty</c> target; only plain CLR property targets
/// (e.g. <c>Run.Text</c>) go through <see cref="DynamicKeyBinder"/> instead.
/// </summary>
/// <param name="request">The lookup settings used to resolve each bound key.</param>
internal sealed class DynamicKeyLocalizationConverter(LocalizationRequest request) : IMultiValueConverter
{
    /// <summary>
    /// Creates a converter that resolves keys against a single assembly.
    /// </summary>
    /// <param name="service">The service lookups go through.</param>
    /// <param name="assembly">The assembly unqualified keys resolve against.</param>
    /// <param name="keyPrefix">Optional text prepended to each bound key before lookup.</param>
    public DynamicKeyLocalizationConverter(ILocalizationService service, Assembly assembly, string? keyPrefix = null)
        : this(new LocalizationRequest(service, assembly, keyPrefix: keyPrefix))
    {
    }

    /// <summary>
    /// Resolves the first bound value as a resource key.
    /// </summary>
    /// <param name="values">The bound values; the first is the key, the second is the culture trigger.</param>
    /// <param name="targetType">Ignored.</param>
    /// <param name="parameter">Ignored.</param>
    /// <param name="culture">Ignored; the localization service's culture is used instead.</param>
    /// <returns>The localized text, or an empty string when there is no key.</returns>
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        // Unlike a binding to a string DependencyProperty (as DynamicKeyBinder uses for CLR
        // property targets), a MultiBinding's child bindings hand their raw source value straight
        // to the converter with no implicit target-type conversion — so a non-string KeyBinding
        // source (e.g. an enum, as from {Binding SomeEnumProperty}) arrives here as the boxed
        // enum, not its name. A plain `as string` cast then silently misses on every row,
        // producing an empty string with no binding error and no failed-lookup warning to
        // explain it. ToString() matches what WPF's own implicit conversion would have produced.
        // ResolveBoundKey also covers the "no key" values a failed child binding produces
        // (UnsetValue / DoNothing), which must not be stringified into a bogus key.
        return request.ResolveBoundKey(values.Length > 0 ? values[0] : null);
    }

    /// <summary>Not supported; localized text cannot be converted back to a key.</summary>
    /// <param name="value">Unused.</param>
    /// <param name="targetTypes">Unused.</param>
    /// <param name="parameter">Unused.</param>
    /// <param name="culture">Unused.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException($"{nameof(DynamicKeyLocalizationConverter)} only supports one-way binding.");
}
