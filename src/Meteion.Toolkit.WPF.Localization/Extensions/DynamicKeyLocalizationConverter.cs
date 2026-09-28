using Meteion.Toolkit.Localization.Abstractions;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;

namespace Meteion.Toolkit.WPF.Localization.Extensions;

/// <summary>
/// <see cref="MultiBinding.Converter"/> that resolves a <see cref="LocalizedValueExtension.KeyBinding"/>
/// value into localized text. Combined with a <see cref="CultureChangeTrigger"/> as the second
/// input, so the result re-resolves whenever either the bound key or the active culture changes.
/// </summary>
internal sealed class DynamicKeyLocalizationConverter(LocalizationRequest request) : IMultiValueConverter
{
    public DynamicKeyLocalizationConverter(ILocalizationService service, Assembly assembly, string? keyPrefix = null)
        : this(new LocalizationRequest(service, assembly, keyPrefix: keyPrefix))
    {
    }

    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        // Unlike a real DependencyProperty binding (see DynamicKeyBinder), a MultiBinding's
        // child bindings hand their raw source value straight to the converter with no
        // implicit target-type conversion — so a non-string KeyBinding source (e.g. an enum,
        // as from {Binding SomeEnumProperty}) arrives here as the boxed enum, not its name.
        // A plain `as string` cast then silently misses on every row, producing an empty
        // string with no binding error and no failed-lookup warning to explain it. ToString()
        // matches what WPF's own implicit conversion would have produced for the DP case.
        // ResolveBoundKey also covers the "no key" values a failed child binding produces
        // (UnsetValue / DoNothing), which must not be stringified into a bogus key.
        return request.ResolveBoundKey(values.Length > 0 ? values[0] : null);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException($"{nameof(DynamicKeyLocalizationConverter)} only supports one-way binding.");
}
